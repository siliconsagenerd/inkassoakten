using InkassoAkten.Application.Interfaces;
using InkassoAkten.Infrastructure;
using InkassoAkten.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOperatorContext, OperatorContext>();
builder.Services.AddScoped<IOperatorContextInitializer>(sp => (IOperatorContextInitializer)sp.GetRequiredService<IOperatorContext>());

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "InkassoAkten.Operator";
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Operator", policy => policy.RequireRole("Operator"));
});

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await Task.CompletedTask;
    };
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = builder.Configuration.GetValue<int>("RateLimits:LoginPerMinute", 5);
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("pdf", o =>
    {
        o.PermitLimit = builder.Configuration.GetValue<int>("RateLimits:PdfPerMinute", 30);
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=InkassoDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";

builder.Services.AddDbContext<InkassoAktenDbContext>(options =>
    options.UseSqlServer(connectionString));

// Application & Infrastructure Services
builder.Services.AddScoped<IPdfStatementService, PdfStatementService>();
builder.Services.AddScoped<IAgingReportService, AgingReportService>();
builder.Services.AddScoped<ZahlungPostingService>();

var seedDemo = args.Any(a => string.Equals(a, "--seed-demo", StringComparison.OrdinalIgnoreCase));

var app = builder.Build();

if (seedDemo)
{
    using var scope = app.Services.CreateScope();

    // Explicit, non-web seed execution scope: bypass the web operator guard only here.
    // Resolve and configure the operator context BEFORE the DbContext so the bypass
    // is in place on the same scoped instance the DbContext captures.
    var operatorContext = scope.ServiceProvider.GetService<IOperatorContextInitializer>();
    if (operatorContext is not null)
    {
        operatorContext.BypassWrites = true;
    }

    var db = scope.ServiceProvider.GetRequiredService<InkassoAktenDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations before demo seed.");
        await db.Database.MigrateAsync();

        logger.LogInformation("Seeding synthetic demo data if the database is empty.");
        await DatabaseSeeder.SeedDemoAsync(db);

        logger.LogInformation("Demo seed completed. No credentials or connection strings were logged.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Demo seed failed. No partial seed data should remain.");
        throw;
    }
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok", app = "InkassoAkten" }));

// PDF Download Endpoint
app.MapGet("/api/akten/{id:int}/pdf", async (int id, InkassoAktenDbContext db, IPdfStatementService pdfService) =>
{
    var akte = await db.InkassoAkten
        .Include(a => a.Mandant)
        .Include(a => a.Schuldner)
        .Include(a => a.Forderung)
        .FirstOrDefaultAsync(a => a.AkteId == id);

    if (akte is null) return Results.NotFound();
    if (akte.Mandant is null || akte.Schuldner is null || akte.Forderung is null)
        return Results.Problem("Case data is incomplete.", statusCode: 500);

    var buchungen = await db.Buchungen
        .Where(b => b.AkteId == id)
        .OrderBy(b => b.CreatedAtUtc)
        .Select(b => new ForderungsaufstellungBuchung(
            b.BuchungId,
            b.CreatedAtUtc,
            b.BetragKosten,
            b.BetragZinsen,
            b.BetragHauptforderung,
            b.BetragKosten + b.BetragZinsen + b.BetragHauptforderung))
        .ToListAsync();

    var data = new ForderungsaufstellungData(
        Aktenzeichen: akte.Aktenzeichen,
        MandantName: akte.Mandant.Firmenname,
        SchuldnerName: $"{akte.Schuldner.NachnameOderFirma}, {akte.Schuldner.Vorname}".Trim(' ', ','),
        SchuldnerAdresse: $"{akte.Schuldner.Strasse}, {akte.Schuldner.PLZ} {akte.Schuldner.Ort}",
        Verzugsbeginn: akte.Forderung.Faelligkeitsdatum,
        UrspruenglicheHauptforderung: akte.Forderung.Hauptforderung,
        OffeneHauptforderung: akte.OffeneHauptforderung,
        OffeneZinsen: akte.OffeneZinsen,
        OffeneKosten: akte.OffeneKosten,
        Gesamtsaldo: akte.Gesamtsaldo,
        Buchungen: buchungen,
        ErstelltAmUtc: DateTime.UtcNow);

    var bytes = pdfService.GeneriereForderungsaufstellung(data);
    return Results.File(bytes, "application/pdf", $"Forderungsaufstellung_{akte.Aktenzeichen}.pdf");
}).RequireRateLimiting("pdf");

// Authentication endpoints (cookie-based operator login)
app.MapPost("/auth/login", async (HttpContext http, IConfiguration config, IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(http);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest();
    }

    var form = await http.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var expectedUser = config["Auth:Operator:Username"] ?? "operator";
    var expectedPass = config["Auth:Operator:Password"] ?? "";

    var publicReadOnly = config.GetValue<bool>("Security:PublicReadOnly", false);
    var ok = !publicReadOnly
        && !string.IsNullOrEmpty(expectedPass)
        && string.Equals(username, expectedUser, StringComparison.Ordinal)
        && string.Equals(password, expectedPass, StringComparison.Ordinal);

    if (!ok)
    {
        return Results.Redirect("/login?error=1");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, username),
        new(ClaimTypes.Role, "Operator")
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Redirect("/");
}).RequireRateLimiting("login");

app.MapPost("/auth/logout", async (HttpContext http, IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(http);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest();
    }

    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapRazorComponents<InkassoAkten.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program { }
