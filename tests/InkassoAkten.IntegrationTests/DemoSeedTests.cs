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

    [Fact]
    public async Task SeedDemo_FirstRun_CreatesFiveCases()
    {
        using var context = CreateContext();
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
        try { await context.Database.EnsureDeletedAsync(); } catch { }
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
        using var context = CreateContext();
        try { await context.Database.EnsureDeletedAsync(); } catch { }
        // Do not migrate: schema missing.

        Func<Task> act = () => DatabaseSeeder.SeedDemoAsync(context);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
