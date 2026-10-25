namespace InkassoAkten.Domain.ValueObjects;

public record TilgungsAllocation(
    decimal GetilgteKosten,
    decimal GetilgteZinsen,
    decimal GetilgteHauptforderung,
    decimal NeuerSaldo)
{
    public decimal Gesamtbetrag => GetilgteKosten + GetilgteZinsen + GetilgteHauptforderung;
}
