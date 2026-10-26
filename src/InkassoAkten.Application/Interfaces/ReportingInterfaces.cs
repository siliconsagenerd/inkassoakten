namespace InkassoAkten.Application.Interfaces;

public record ForderungsAltersstrukturRow(
    int AkteId,
    string Akte,
    string Schuldner,
    string Mandant,
    decimal Hauptforderung,
    decimal OffeneHauptforderung,
    decimal OffeneZinsen,
    decimal OffeneKosten,
    decimal Gesamtsaldo,
    int TageUeberfaellig,
    string Altersbucket);

public record ForderungsaufstellungData(
    string Aktenzeichen,
    string MandantName,
    string SchuldnerName,
    string SchuldnerAdresse,
    DateOnly Verzugsbeginn,
    decimal UrspruenglicheHauptforderung,
    decimal OffeneHauptforderung,
    decimal OffeneZinsen,
    decimal OffeneKosten,
    decimal Gesamtsaldo,
    IReadOnlyList<ForderungsaufstellungBuchung> Buchungen,
    DateTime ErstelltAmUtc);

public record ForderungsaufstellungBuchung(
    long BuchungId,
    DateTime DatumUtc,
    decimal Kosten,
    decimal Zinsen,
    decimal Hauptforderung,
    decimal Gesamt);

public interface IPdfStatementService
{
    byte[] GeneriereForderungsaufstellung(ForderungsaufstellungData data);
}

public interface IAgingReportService
{
    Task<IReadOnlyList<ForderungsAltersstrukturRow>> GetAltersstrukturAsync(CancellationToken ct = default);
}
