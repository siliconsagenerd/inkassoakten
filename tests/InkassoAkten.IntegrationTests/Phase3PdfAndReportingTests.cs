using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InkassoAkten.Application.Interfaces;
using InkassoAkten.Domain.Entities;
using InkassoAkten.Infrastructure;
using Xunit;

namespace InkassoAkten.IntegrationTests;

[Collection("SqlServer")]
public class Phase3PdfAndReportingTests
{
    private readonly SqlServerFixture _fixture;

    public Phase3PdfAndReportingTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Pdf_Statement_Generates_Non_Empty_Pdf()
    {
        var service = new PdfStatementService();
        var data = new ForderungsaufstellungData(
            Aktenzeichen: "ADV-2026-TEST",
            MandantName: "Test Gläubiger GmbH",
            SchuldnerName: "Erika Mustermann",
            SchuldnerAdresse: "Musterstr. 1, 10115 Berlin",
            Verzugsbeginn: new DateOnly(2026, 1, 15),
            UrspruenglicheHauptforderung: 1500.00m,
            OffeneHauptforderung: 1200.00m,
            OffeneZinsen: 45.60m,
            OffeneKosten: 70.20m,
            Gesamtsaldo: 1315.80m,
            Buchungen: new List<ForderungsaufstellungBuchung>
            {
                new(1, DateTime.UtcNow.AddDays(-10), 30m, 10m, 300m, 340m)
            },
            ErstelltAmUtc: DateTime.UtcNow);

        var bytes = service.GeneriereForderungsaufstellung(data);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1024);

        var header = System.Text.Encoding.ASCII.GetString(bytes.Take(5).ToArray());
        header.Should().Be("%PDF-");
    }

    [Fact]
    public async Task Pdf_Endpoint_Returns_Application_Pdf_And_Non_Empty_Content()
    {
        var conn = _fixture.ConnectionString;

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", conn);
            });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InkassoAktenDbContext>();
            await db.Database.MigrateAsync();
            await DatabaseSeeder.SeedAsync(db);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/akten/1/pdf");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1024);

        var header = System.Text.Encoding.ASCII.GetString(bytes.Take(5).ToArray());
        header.Should().Be("%PDF-");
    }

    [Fact]
    public async Task Aging_Report_Buckets_Known_Dates_Correctly()
    {
        var conn = _fixture.ConnectionString;

        var options = new DbContextOptionsBuilder<InkassoAktenDbContext>()
            .UseSqlServer(conn)
            .Options;

        using var db = new InkassoAktenDbContext(options);
        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);

        var reportService = new AgingReportService(db);
        var rows = await reportService.GetAltersstrukturAsync();

        rows.Should().NotBeEmpty();
        var buckets = rows.Select(r => r.Altersbucket).Distinct().ToList();
        buckets.Should().Contain(new[] { "1-30", "31-60", "61-90" });
        foreach (var r in rows)
        {
            r.Akte.Should().NotBeNullOrWhiteSpace();
            r.Schuldner.Should().NotBeNullOrWhiteSpace();
            r.Mandant.Should().NotBeNullOrWhiteSpace();
            r.Gesamtsaldo.Should().Be(r.OffeneHauptforderung + r.OffeneZinsen + r.OffeneKosten);

            if (r.TageUeberfaellig > 90) r.Altersbucket.Should().Be("90+");
            else if (r.TageUeberfaellig >= 61 && r.TageUeberfaellig <= 90) r.Altersbucket.Should().Be("61-90");
            else if (r.TageUeberfaellig >= 31 && r.TageUeberfaellig <= 60) r.Altersbucket.Should().Be("31-60");
            else if (r.TageUeberfaellig >= 1 && r.TageUeberfaellig <= 30) r.Altersbucket.Should().Be("1-30");
            else r.Altersbucket.Should().Be("0 (aktuell)");
        }
    }

    [Theory]
    [InlineData(0, "0 (aktuell)")]
    [InlineData(1, "1-30")]
    [InlineData(30, "1-30")]
    [InlineData(31, "31-60")]
    [InlineData(60, "31-60")]
    [InlineData(61, "61-90")]
    [InlineData(90, "61-90")]
    [InlineData(91, "90+")]
    public async Task Aging_Report_Bucket_Boundaries(int daysOverdue, string expectedBucket)
    {
        var conn = _fixture.ConnectionString;

        var options = new DbContextOptionsBuilder<InkassoAktenDbContext>()
            .UseSqlServer(conn)
            .Options;

        using var db = new InkassoAktenDbContext(options);
        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);

        var mandant = new Mandant { Firmenname = "Boundary Mandant", IstAktiv = true };
        var schuldner = new Schuldner { SchuldnerTyp = SchuldnerTyp.VerbraucherB2C, NachnameOderFirma = "Boundary", Vorname = "Test", Strasse = "Str. 1", PLZ = "10115", Ort = "Berlin" };
        db.Mandanten.Add(mandant);
        db.Schuldner.Add(schuldner);
        await db.SaveChangesAsync();

        var forderung = new Forderung
        {
            MandantId = mandant.MandantId,
            SchuldnerId = schuldner.SchuldnerId,
            Rechnungsnummer = $"BOUNDARY-{daysOverdue}",
            Hauptforderung = 100m,
            Faelligkeitsdatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-daysOverdue)),
            Waehrung = "EUR"
        };
        db.Forderungen.Add(forderung);
        await db.SaveChangesAsync();

        var akte = new InkassoAkte
        {
            Aktenzeichen = $"B{daysOverdue}-{Guid.NewGuid().ToString("N")[..8]}",
            MandantId = mandant.MandantId,
            SchuldnerId = schuldner.SchuldnerId,
            ForderungId = forderung.ForderungId,
            AkteStatus = AkteStatus.Aktenanlage,
            VerzugsbeginnDatum = forderung.Faelligkeitsdatum,
            ZinsenBerechnetBis = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        akte.SetzeOffeneBetrage(0m, 0m, 100m);
        db.InkassoAkten.Add(akte);
        await db.SaveChangesAsync();

        var reportService = new AgingReportService(db);
        var rows = await reportService.GetAltersstrukturAsync();
        var row = rows.Single(r => r.Akte == akte.Aktenzeichen);

        row.TageUeberfaellig.Should().Be(daysOverdue);
        row.Altersbucket.Should().Be(expectedBucket);
    }
}
