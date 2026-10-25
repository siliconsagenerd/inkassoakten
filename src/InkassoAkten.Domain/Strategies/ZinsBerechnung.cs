using InkassoAkten.Domain.Entities;
using InkassoAkten.Domain.ValueObjects;

namespace InkassoAkten.Domain.Strategies;

public class StandardZinsBerechnungsStrategie : IZinsBerechnungsStrategie
{
    public ZinsStaffel Berechne(
        decimal kapital,
        DateTime verzugsbeginn,
        DateTime berechnetBis,
        SchuldnerTyp schuldnerTyp,
        IEnumerable<BasiszinsPeriode> basiszinsHistorie)
    {
        var perioden = new List<ZinsPeriode>();
        if (berechnetBis <= verzugsbeginn) return new ZinsStaffel(perioden, 0m);

        var aufschlag = schuldnerTyp == SchuldnerTyp.UnternehmerB2B ? 9m : 5m;
        var historie = basiszinsHistorie.OrderBy(b => b.GueltigAb).ToList();

        var cursor = verzugsbeginn.Date;
        var ende = berechnetBis.Date;
        decimal gesamt = 0m;

        while (cursor < ende)
        {
            // find applicable basiszins period
            var bz = historie
                .Where(b => b.GueltigAb <= DateOnly.FromDateTime(cursor))
                .OrderByDescending(b => b.GueltigAb)
                .FirstOrDefault();
            if (bz is null)
            {
                // no applicable period; advance one day to avoid infinite loop
                cursor = cursor.AddDays(1);
                continue;
            }

            // determine next change date
            var nextChange = historie
                .Where(b => b.GueltigAb > DateOnly.FromDateTime(cursor))
                .OrderBy(b => b.GueltigAb)
                .FirstOrDefault();
            var segmentEnd = nextChange is null ? ende : nextChange.GueltigAb.ToDateTime(TimeOnly.MinValue);
            if (segmentEnd > ende) segmentEnd = ende;
            if (segmentEnd <= cursor) segmentEnd = cursor.AddDays(1);

            var tage = (segmentEnd - cursor).Days;
            if (tage < 0) tage = 0;
            var zinssatz = bz.Basiszinssatz + aufschlag;
            var zinsen = decimal.Round(kapital * (zinssatz / 100m) * tage / 365m, 2, MidpointRounding.AwayFromZero);
            gesamt = decimal.Round(gesamt + zinsen, 2, MidpointRounding.AwayFromZero);

            perioden.Add(new ZinsPeriode(cursor, segmentEnd, tage, bz.Basiszinssatz, aufschlag, zinssatz, zinsen));
            cursor = segmentEnd;
        }

        return new ZinsStaffel(perioden, gesamt);
    }
}
