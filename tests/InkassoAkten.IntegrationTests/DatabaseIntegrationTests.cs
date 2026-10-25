using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Testcontainers.MsSql;
using InkassoAkten.Infrastructure;
using InkassoAkten.Domain.Entities;
using FluentAssertions;

namespace InkassoAkten.IntegrationTests;

public class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var overrideConn = Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTIONSTRING");
        if (!string.IsNullOrWhiteSpace(overrideConn))
        {
            ConnectionString = overrideConn;
            return;
        }
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}

[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture> { }

[Collection("SqlServer")]
public class DatabaseIntegrationTests
{
    private readonly SqlServerFixture _fixture;

    public DatabaseIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_Connect_To_Real_SqlServer()
    {
        using var context = CreateContext();
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task Migrations_Apply_To_Empty_Database()
    {
        using var context = CreateContext();
        await context.Database.MigrateAsync();
        var applied = await context.Database.GetAppliedMigrationsAsync();
        applied.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Seed_Creates_Exactly_Five_InkassoAkten()
    {
        using var context = CreateContext();
        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context);
        var count = await context.InkassoAkten.CountAsync();
        count.Should().Be(5);
    }

    [Fact]
    public async Task Concurrent_Update_Raises_Concurrency_Exception()
    {
        using var context1 = CreateContext();
        using var context2 = CreateContext();
        await context1.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context1);

        var akte1 = await context1.InkassoAkten.FirstAsync();
        var akte2 = await context2.InkassoAkten.FirstAsync(a => a.AkteId == akte1.AkteId);

        akte1.AkteStatus = AkteStatus.VorgerichtlicheMahnung;
        await context1.SaveChangesAsync();

        akte2.AkteStatus = AkteStatus.Abgeschlossen;
        Func<Task> act = async () => await context2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    private InkassoAktenDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InkassoAktenDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;
        return new InkassoAktenDbContext(options);
    }
}
