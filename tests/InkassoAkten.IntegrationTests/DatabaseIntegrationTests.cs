using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Testcontainers.MsSql;
using InkassoAkten.Infrastructure;
using InkassoAkten.Domain.Entities;
using InkassoAkten.Domain.Strategies;
using InkassoAkten.Domain.Exceptions;
using InkassoAkten.Domain.ValueObjects;
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

        akte2.AkteStatus = AkteStatus.Erledigt;
        Func<Task> act = async () => await context2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task Payment_Posting_Creates_Ledger_Rows()
    {
        using var context = CreateContext();
        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context);

        var akte = await context.InkassoAkten.FirstAsync(a => a.Aktenzeichen == "ADV-2026-0002");
        var service = new ZahlungPostingService(context);
        var zahlung = await service.PosteZahlungAsync(akte.AkteId, 100m, DateTime.UtcNow, new StandardTilgungsStrategie());

        var buchungen = await context.Buchungen.Where(b => b.ZahlungId == zahlung.ZahlungId).ToListAsync();
        buchungen.Should().HaveCount(1);
        buchungen[0].BetragKosten.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public async Task Payment_Allocation_Sums_Exactly()
    {
        using var context = CreateContext();
        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context);

        var akte = await context.InkassoAkten.FirstAsync(a => a.Aktenzeichen == "ADV-2026-0002");
        var service = new ZahlungPostingService(context);
        var zahlung = await service.PosteZahlungAsync(akte.AkteId, 100m, DateTime.UtcNow, new StandardTilgungsStrategie());

        var buchung = await context.Buchungen.FirstAsync(b => b.ZahlungId == zahlung.ZahlungId);
        (buchung.BetragKosten + buchung.BetragZinsen + buchung.BetragHauptforderung).Should().Be(zahlung.Betrag);
    }

    [Fact]
    public async Task Concurrent_Payments_Do_Not_Double_Allocate()
    {
        using var context1 = CreateContext();
        using var context2 = CreateContext();
        await context1.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context1);

        var akte = await context1.InkassoAkten.FirstAsync();
        var id = akte.AkteId;
        var offener = akte.Gesamtsaldo;
        var betrag = offener; // full balance: second concurrent attempt must fail

        var s1 = new ZahlungPostingService(context1);
        var s2 = new ZahlungPostingService(context2);

        var t1 = Task.Run(async () => { try { await s1.PosteZahlungAsync(id, betrag, DateTime.UtcNow, new StandardTilgungsStrategie()); return true; } catch { return false; } });
        var t2 = Task.Run(async () => { try { await s2.PosteZahlungAsync(id, betrag, DateTime.UtcNow, new StandardTilgungsStrategie()); return true; } catch { return false; } });

        var results = await Task.WhenAll(t1, t2);
        results.Count(r => r).Should().Be(1);
    }

    [Fact]
    public async Task Audit_Entry_Written_With_Payment()
    {
        using var context = CreateContext();
        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context);

        var akte = await context.InkassoAkten.FirstAsync(a => a.Aktenzeichen == "ADV-2026-0002");
        var service = new ZahlungPostingService(context);
        await service.PosteZahlungAsync(akte.AkteId, 50m, DateTime.UtcNow, new StandardTilgungsStrategie());

        var audits = await context.AuditEintraege.Where(a => a.Aktion == "PaymentPosted").ToListAsync();
        audits.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Invalid_State_Transition_Rejected_Integration()
    {
        using var context = CreateContext();
        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context);

        var akte = await context.InkassoAkten.FirstAsync();
        akte.AkteStatus = AkteStatus.Aktenanlage; // force known state in-memory
        Action act = () => akte.AktiviereRatenplan();
        act.Should().Throw<InvalidTransitionException>();
    }

    [Fact]
    public async Task Akte_Cannot_Close_With_Positive_Balance_Integration()
    {
        using var context = CreateContext();
        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context);

        var akte = await context.InkassoAkten.FirstAsync();
        akte.SetzeOffeneBetrage(0m, 0m, 100m);
        akte.AkteStatus = AkteStatus.Erledigt;
        Action act = () => akte.Schliesse();
        act.Should().Throw<InvalidTransitionException>();
    }

    private InkassoAktenDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InkassoAktenDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;
        return new InkassoAktenDbContext(options);
    }
}
