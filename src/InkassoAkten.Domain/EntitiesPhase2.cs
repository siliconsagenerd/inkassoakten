using InkassoAkten.Domain.Exceptions;

namespace InkassoAkten.Domain.Entities;

public class BasiszinsPeriode
{
    public int BasiszinsPeriodeId { get; set; }
    public DateOnly GueltigAb { get; set; }
    public decimal Basiszinssatz { get; set; }
}

public class Zahlung
{
    public int ZahlungId { get; set; }
    public int AkteId { get; set; }
    public decimal Betrag { get; set; }
    public DateTime ZahlungsdatumUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class Buchung
{
    public long BuchungId { get; set; }
    public int ZahlungId { get; set; }
    public int AkteId { get; set; }
    public decimal BetragKosten { get; set; }
    public decimal BetragZinsen { get; set; }
    public decimal BetragHauptforderung { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Ratenzahlungsplan
{
    public int PlanId { get; set; }
    public int AkteId { get; set; }
    public decimal Gesamtbetrag { get; set; }
    public bool IstDefaulted { get; set; }
    public DateTime? DefaultedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<Rate> Raten { get; set; } = new();

    public void ValidierePlan()
    {
        if (Gesamtbetrag <= 0m)
            throw new DomainException("Plan must cover a positive balance.");
        if (Raten.Count == 0)
            throw new DomainException("Plan must contain at least one installment.");
        foreach (var r in Raten)
        {
            if (r.Betrag <= 0m)
                throw new DomainException("Installment amount must be positive.");
        }
        // non-overlapping due dates
        var dates = Raten.Select(r => r.Faelligkeitsdatum).ToList();
        if (dates.Distinct().Count() != dates.Count)
            throw new DomainException("Installment due dates must not overlap.");
        // cover balance
        var summe = Raten.Sum(r => r.Betrag);
        if (summe < Gesamtbetrag)
            throw new DomainException("Installments must cover the outstanding balance.");
    }

    public void PruefeDefault(DateTime heute)
    {
        if (IstDefaulted) return;
        foreach (var r in Raten)
        {
            if (!r.IstBezahlt && r.Faelligkeitsdatum < DateOnly.FromDateTime(heute))
            {
                IstDefaulted = true;
                DefaultedAtUtc = DateTime.UtcNow;
                return;
            }
        }
    }
}

public class Rate
{
    public int RateId { get; set; }
    public int PlanId { get; set; }
    public DateOnly Faelligkeitsdatum { get; set; }
    public decimal Betrag { get; set; }
    public bool IstBezahlt { get; set; }
    public DateTime? BezahltAmUtc { get; set; }
}
