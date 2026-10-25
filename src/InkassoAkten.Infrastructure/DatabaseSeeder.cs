using Microsoft.EntityFrameworkCore;
using InkassoAkten.Domain.Entities;

namespace InkassoAkten.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(InkassoAktenDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.InkassoAkten.AnyAsync(cancellationToken))
        {
            return;
        }

        var mandant1 = new Mandant { Firmenname = "Beispiel GmbH", IstAktiv = true };
        var mandant2 = new Mandant { Firmenname = "Musterhandel AG", IstAktiv = true };
        context.Mandanten.AddRange(mandant1, mandant2);
        await context.SaveChangesAsync(cancellationToken);

        var schuldner1 = new Schuldner { SchuldnerTyp = SchuldnerTyp.VerbraucherB2C, NachnameOderFirma = "Beispiel", Vorname = "Anna", Strasse = "Musterstr. 1", PLZ = "10115", Ort = "Berlin" };
        var schuldner2 = new Schuldner { SchuldnerTyp = SchuldnerTyp.UnternehmerB2B, NachnameOderFirma = "Beispiel", Vorname = "David", Strasse = "Hauptstr. 42", PLZ = "20354", Ort = "Hamburg" };
        var schuldner3 = new Schuldner { SchuldnerTyp = SchuldnerTyp.VerbraucherB2C, NachnameOderFirma = "Beispiel", Vorname = "Maria", Strasse = "Kaufingerstr. 12", PLZ = "80331", Ort = "München" };
        var schuldner4 = new Schuldner { SchuldnerTyp = SchuldnerTyp.UnternehmerB2B, NachnameOderFirma = "Musterfirma UG", Vorname = null, Strasse = "Aachener Str. 5", PLZ = "50667", Ort = "Köln" };
        var schuldner5 = new Schuldner { SchuldnerTyp = SchuldnerTyp.VerbraucherB2C, NachnameOderFirma = "Testmann", Vorname = "Tom", Strasse = "Bahnhofstr. 10", PLZ = "66111", Ort = "Saarbrücken" };
        context.Schuldner.AddRange(schuldner1, schuldner2, schuldner3, schuldner4, schuldner5);
        await context.SaveChangesAsync(cancellationToken);

        var f1 = new Forderung { MandantId = mandant1.MandantId, SchuldnerId = schuldner1.SchuldnerId, Rechnungsnummer = "RE-2026-0001", Hauptforderung = 1500.00m, Faelligkeitsdatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), Waehrung = "EUR" };
        var f2 = new Forderung { MandantId = mandant1.MandantId, SchuldnerId = schuldner2.SchuldnerId, Rechnungsnummer = "RE-2026-0002", Hauptforderung = 3450.50m, Faelligkeitsdatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)), Waehrung = "EUR" };
        var f3 = new Forderung { MandantId = mandant2.MandantId, SchuldnerId = schuldner3.SchuldnerId, Rechnungsnummer = "RE-2026-0003", Hauptforderung = 890.00m, Faelligkeitsdatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-15)), Waehrung = "EUR" };
        var f4 = new Forderung { MandantId = mandant2.MandantId, SchuldnerId = schuldner4.SchuldnerId, Rechnungsnummer = "RE-2026-0004", Hauptforderung = 12500.00m, Faelligkeitsdatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90)), Waehrung = "EUR" };
        var f5 = new Forderung { MandantId = mandant1.MandantId, SchuldnerId = schuldner5.SchuldnerId, Rechnungsnummer = "RE-2026-0005", Hauptforderung = 450.00m, Faelligkeitsdatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), Waehrung = "EUR" };
        context.Forderungen.AddRange(f1, f2, f3, f4, f5);
        await context.SaveChangesAsync(cancellationToken);

        var a1 = new InkassoAkte { Aktenzeichen = "ADV-2026-0001", MandantId = mandant1.MandantId, SchuldnerId = schuldner1.SchuldnerId, ForderungId = f1.ForderungId, AkteStatus = AkteStatus.Aktenanlage, VerzugsbeginnDatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), ZinsenBerechnetBis = DateOnly.FromDateTime(DateTime.UtcNow) };
        var a2 = new InkassoAkte { Aktenzeichen = "ADV-2026-0002", MandantId = mandant1.MandantId, SchuldnerId = schuldner2.SchuldnerId, ForderungId = f2.ForderungId, AkteStatus = AkteStatus.VorgerichtlicheMahnung, VerzugsbeginnDatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)), ZinsenBerechnetBis = DateOnly.FromDateTime(DateTime.UtcNow) };
        var a3 = new InkassoAkte { Aktenzeichen = "ADV-2026-0003", MandantId = mandant2.MandantId, SchuldnerId = schuldner3.SchuldnerId, ForderungId = f3.ForderungId, AkteStatus = AkteStatus.Aktenanlage, VerzugsbeginnDatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-15)), ZinsenBerechnetBis = DateOnly.FromDateTime(DateTime.UtcNow) };
        var a4 = new InkassoAkte { Aktenzeichen = "ADV-2026-0004", MandantId = mandant2.MandantId, SchuldnerId = schuldner4.SchuldnerId, ForderungId = f4.ForderungId, AkteStatus = AkteStatus.RatenplanAktiv, VerzugsbeginnDatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90)), ZinsenBerechnetBis = DateOnly.FromDateTime(DateTime.UtcNow) };
        var a5 = new InkassoAkte { Aktenzeichen = "ADV-2026-0005", MandantId = mandant1.MandantId, SchuldnerId = schuldner5.SchuldnerId, ForderungId = f5.ForderungId, AkteStatus = AkteStatus.Aktenanlage, VerzugsbeginnDatum = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), ZinsenBerechnetBis = DateOnly.FromDateTime(DateTime.UtcNow) };
        context.InkassoAkten.AddRange(a1, a2, a3, a4, a5);
        await context.SaveChangesAsync(cancellationToken);
    }
}
