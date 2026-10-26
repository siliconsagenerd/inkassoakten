using FluentAssertions;
using InkassoAkten.Domain.Entities;
using InkassoAkten.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InkassoAkten.IntegrationTests;

[Collection("SqlServer")]
public class DemoSeedTests
{
    private readonly SqlServerFixture _fixture;

    public DemoSeedTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private InkassoAktenDbContext CreateContext()
    {
        TestDatabaseGuard.AssertSafeForDestructiveTests(_fixture.ConnectionString);
        var options = new DbContextOptionsBuilder<InkassoAktenDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;
        return new InkassoAktenDbContext(options);
    }

    private static async Task ResetAsync(InkassoAktenDbContext context)
    {
        // Testcontainers uses the shared 'master' database, where EnsureDeletedAsync
        // cannot run (SINGLE_USER is forbidden on master). Clear rows directly instead.
        var deletes = new[]
        {
            "DELETE FROM AuditEintraege",
            "DELETE FROM Raten",
            "DELETE FROM Ratenzahlungsplaene",
            "DELETE FROM Buchungen",
            "DELETE FROM Zahlungen",
            "DELETE FROM Mahnungen",
            "DELETE FROM InkassoAkten",
            "DELETE FROM Forderungen",
            "DELETE FROM Schuldner",
            "DELETE FROM Mandanten"
        };
        foreach (var sql in deletes)
        {
            try { await context.Database.ExecuteSqlRawAsync(sql); } catch { }
        }
    }

    [Fact]
    public async Task SeedDemo_FirstRun_CreatesFiveCases()
    {
        using var context = CreateContext();
        await ResetAsync(context);
        try { await context.Database.EnsureDeletedAsync(); } catch { }
        await context.Database.MigrateAsync();

        await DatabaseSeeder.SeedDemoAsync(context);

        var count = await context.InkassoAkten.CountAsync();
        count.Should().Be(5);
    }

    [Fact]
    public async Task SeedDemo_RepeatedRun_IsIdempotent()
    {
        using var context = CreateContext();
        await ResetAsync(context);
        await context.Database.MigrateAsync();

        await DatabaseSeeder.SeedDemoAsync(context);
        await DatabaseSeeder.SeedDemoAsync(context);

        var count = await context.InkassoAkten.CountAsync();
        count.Should().Be(5);
    }

    [Fact]
    public async Task SeedDemo_PreservesLegitimateChanges()
    {
        using var context = CreateContext();
        await ResetAsync(context);
        await context.Database.MigrateAsync();

        await DatabaseSeeder.SeedDemoAsync(context);

        var akte = await context.InkassoAkten.FirstAsync(a => a.Aktenzeichen == "ADV-2026-0001");
        akte.AkteStatus = AkteStatus.Erledigt;
        akte.SetzeOffeneBetrage(akte.OffeneKosten, akte.OffeneZinsen, 42m);
        await context.SaveChangesAsync();

        await DatabaseSeeder.SeedDemoAsync(context);

        var reloaded = await context.InkassoAkten.FirstAsync(a => a.Aktenzeichen == "ADV-2026-0001");
        reloaded.AkteStatus.Should().Be(AkteStatus.Erledigt);
        reloaded.OffeneHauptforderung.Should().Be(42m);
    }

    [Fact]
    public async Task SeedDemo_RejectsPartialData()
    {
        using var context = CreateContext();
        await ResetAsync(context);
        await context.Database.MigrateAsync();

        await DatabaseSeeder.SeedDemoAsync(context);

        var toRemove = await context.InkassoAkten.OrderBy(a => a.AkteId).FirstAsync();
        context.InkassoAkten.Remove(toRemove);
        await context.SaveChangesAsync();

        Func<Task> act = () => DatabaseSeeder.SeedDemoAsync(context);
        await act.Should().ThrowAsync<InvalidOperationException>();

        var count = await context.InkassoAkten.CountAsync();
        count.Should().Be(4);
    }

    [Fact]
    public async Task SeedDemo_RefusesIncompatibleExistingData()
    {
        using var context = CreateContext();
        await ResetAsync(context);
        await context.Database.MigrateAsync();

        await DatabaseSeeder.SeedDemoAsync(context);

        var akte = await context.InkassoAkten.FirstAsync();
        akte.Aktenzeichen = "CUSTOM-001";
        await context.SaveChangesAsync();

        Func<Task> act = () => DatabaseSeeder.SeedDemoAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SeedDemo_RequiresMigratedDatabase()
    {
        // Use a database that does not exist to exercise the CanConnect schema-validation branch.
        var conn = _fixture.ConnectionString;
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(conn);
        builder.InitialCatalog = "InkassoAkten_DoesNotExist_" + Guid.NewGuid().ToString("N");
        using var context = new InkassoAktenDbContext(
            new DbContextOptionsBuilder<InkassoAktenDbContext>().UseSqlServer(builder.ConnectionString).Options);

        Func<Task> act = () => DatabaseSeeder.SeedDemoAsync(context);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
