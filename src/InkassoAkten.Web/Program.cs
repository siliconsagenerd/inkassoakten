using InkassoAkten.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=InkassoDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";

builder.Services.AddDbContext<InkassoAktenDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "ok", app = "InkassoAkten" }));

app.MapRazorComponents<InkassoAkten.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
