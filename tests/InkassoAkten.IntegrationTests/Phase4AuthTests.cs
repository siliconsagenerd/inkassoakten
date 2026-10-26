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

    private WebApplicationFactory<Program> CreateFactory(bool requireAuth, bool asOperator, string password = "TestPass!123")
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString);
                builder.UseSetting("Security:RequireOperatorAuth", requireAuth.ToString().ToLower());
                builder.UseSetting("Auth:Operator:Username", "operator");
                builder.UseSetting("Auth:Operator:Password", password);
                if (asOperator)
                {
                    builder.ConfigureServices(services =>
                    {
                        // replace IOperatorContext with an operator-authenticated one
                        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IOperatorContext));
                        if (existing is not null) services.Remove(existing);
                        services.AddScoped<IOperatorContext>(_ => new TestOperatorContext(isOperator: true, requireAuth: requireAuth));
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
    public async Task Login_Succeeds_With_Valid_Credentials()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: false, password: "TestPass!123");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "operator",
            ["password"] = "TestPass!123"
        });
        var response = await client.PostAsync("/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.Contains("InkassoAkten.Operator"));
    }

    [Fact]
    public async Task Login_Fails_With_Invalid_Credentials()
    {
        using var factory = CreateFactory(requireAuth: true, asOperator: false, password: "TestPass!123");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "operator",
            ["password"] = "wrong"
        });
        var response = await client.PostAsync("/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/login");
    }
}

public class TestOperatorContext : IOperatorContext
{
    private readonly bool _isOperator;
    private readonly bool _requireAuth;
    public TestOperatorContext(bool isOperator, bool requireAuth)
    {
        _isOperator = isOperator;
        _requireAuth = requireAuth;
    }
    public bool IsOperator => _isOperator;
    public void SetOperator(bool value) { }
    public bool BypassWrites { get; set; } = false;
    public bool RequireOperatorAuth => _requireAuth;
    public bool CanWrite => !_requireAuth || _isOperator;
}
