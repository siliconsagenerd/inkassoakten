using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InkassoAkten.Application.Interfaces;
using InkassoAkten.Infrastructure;
using InkassoAkten.Domain.Entities;
using System.Net;
using System.Net.Http;
using Xunit;

namespace InkassoAkten.IntegrationTests;

[Collection("SqlServer")]
public class Phase4AuthTests
{
    private readonly SqlServerFixture _fixture;

    public Phase4AuthTests(SqlServerFixture fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> CreateFactory(bool requireAuth, bool asOperator, string password = "TestPass!123", bool setSecurity = true, string environment = "Development", bool publicReadOnly = false)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", environment);
                builder.UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString);
                if (setSecurity)
                {
                    builder.UseSetting("Security:RequireOperatorAuth", requireAuth.ToString().ToLower());
                    builder.UseSetting("Security:PublicReadOnly", publicReadOnly.ToString().ToLower());
                }
                builder.UseSetting("Auth:Operator:Username", "operator");
                builder.UseSetting("Auth:Operator:Password", password);
                if (asOperator)
                {
                    builder.ConfigureServices(services =>
                    {
                        // replace IOperatorContext with an operator-authenticated one
                        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IOperatorContext));
                        if (existing is not null) services.Remove(existing);
                        services.AddScoped<IOperatorContext>(_ => new TestOperatorContext(isOperator: true, requireAuth: requireAuth, publicReadOnly: publicReadOnly));
                    });
                }
            });
    }

    [Fact]
    public async Task Anonymous_Persisted_Mutation_Is_Rejected_When_Auth_Required()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: false);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InkassoAktenDbContext>();

        db.Mandanten.Add(new Mandant { Firmenname = "Anon Test" });
        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Missing_Security_Configuration_Does_Not_Enable_Anonymous_Writes()
    {
        using var factory = CreateFactory(requireAuth: false, asOperator: false, setSecurity: false, environment: "Production");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InkassoAktenDbContext>();

        db.Mandanten.Add(new Mandant { Firmenname = "Missing Config Test" });
        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Operator_Persisted_Mutation_Succeeds_When_Auth_Required()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: true);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InkassoAktenDbContext>();

        db.Mandanten.Add(new Mandant { Firmenname = "Operator Test" });
        var affected = await db.SaveChangesAsync();
        affected.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_Succeeds_With_Valid_Credentials_And_Antiforgery_Token()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: false, password: "TestPass!123");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var getTokenResponse = await client.GetAsync("/login");
        var html = await getTokenResponse.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        match.Success.Should().BeTrue();
        var token = match.Groups[1].Value;

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "operator",
            ["password"] = "TestPass!123",
            ["__RequestVerificationToken"] = token
        });
        var response = await client.PostAsync("/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");
    }

    [Fact]
    public async Task Login_RateLimit_Exhaustion_Returns_429()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: false, password: "TestPass!123");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpStatusCode last = HttpStatusCode.OK;
        for (int i = 0; i < 7; i++)
        {
            var getTokenResponse = await client.GetAsync("/login");
            var html = await getTokenResponse.Content.ReadAsStringAsync();
            var match = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            var token = match.Success ? match.Groups[1].Value : string.Empty;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = "operator",
                ["password"] = "wrong",
                ["__RequestVerificationToken"] = token
            });
            var response = await client.PostAsync("/auth/login", content);
            last = response.StatusCode;
            if (response.StatusCode == HttpStatusCode.TooManyRequests) break;
        }
        last.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_Rejected_Without_Antiforgery_Token()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: false, password: "TestPass!123");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "operator",
            ["password"] = "TestPass!123"
        });
        var response = await client.PostAsync("/auth/login", content);

        // Antiforgery validation fails -> 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Fails_With_Invalid_Credentials()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: false, password: "TestPass!123");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var getTokenResponse = await client.GetAsync("/login");
        var html = await getTokenResponse.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        var token = match.Groups[1].Value;

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "operator",
            ["password"] = "wrong",
            ["__RequestVerificationToken"] = token
        });
        var response = await client.PostAsync("/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/login");
    }

    [Fact]
    public async Task PublicReadOnly_Disables_Operator_Login_Endpoint()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: false, password: "TestPass!123", publicReadOnly: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var getTokenResponse = await client.GetAsync("/login");
        var html = await getTokenResponse.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        var token = match.Groups[1].Value;

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "operator",
            ["password"] = "TestPass!123",
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/auth/login", content);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/login");
    }

    [Fact]
    public async Task PublicReadOnly_Rejects_Persisted_Mutations_Even_For_Operator()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: true, publicReadOnly: true);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InkassoAktenDbContext>();

        db.Mandanten.Add(new Mandant { Firmenname = "Operator In ReadOnly" });
        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task PublicReadOnly_Allows_Anonymous_Reads_And_Pdf()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: false, publicReadOnly: true);
        var client = factory.CreateClient();

        var aktenResp = await client.GetAsync("/akten");
        aktenResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var berichteResp = await client.GetAsync("/berichte");
        berichteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InkassoAktenDbContext>();
        var firstAkte = await db.InkassoAkten.OrderBy(a => a.AkteId).FirstAsync();
        var pdfResp = await client.GetAsync($"/api/akten/{firstAkte.AkteId}/pdf");
        pdfResp.StatusCode.Should().Be(HttpStatusCode.OK);
        pdfResp.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
    }
}

public class TestOperatorContext : IOperatorContext
{
    private readonly bool _isOperator;
    private readonly bool _requireAuth;
    private readonly bool _publicReadOnly;
    public TestOperatorContext(bool isOperator, bool requireAuth, bool publicReadOnly = false)
    {
        _isOperator = isOperator;
        _requireAuth = requireAuth;
        _publicReadOnly = publicReadOnly;
    }
    public bool IsOperator => _isOperator;
    public void SetOperator(bool value) { }
    public bool BypassWrites { get; set; } = false;
    public bool RequireOperatorAuth => _requireAuth;
    public bool PublicReadOnly => _publicReadOnly;
    public bool CanWrite => !_publicReadOnly && (!_requireAuth || _isOperator);
}
