using InkassoAkten.Domain.Exceptions;
using InkassoAkten.Domain.ValueObjects;

namespace InkassoAkten.Domain.Strategies;

public class StandardTilgungsStrategie : ITilgungsVerrechnungsStrategie
{
    public TilgungsAllocation Verrechne(decimal zahlungsbetrag, decimal kosten, decimal zinsen, decimal hauptforderung)
    {
        if (zahlungsbetrag <= 0m)
            throw new DomainException("Payment must be positive.");
        var gesamt = kosten + zinsen + hauptforderung;
        if (zahlungsbetrag > gesamt)
            throw new DomainException("Payment exceeds outstanding balance.");

        var r = MidpointRounding.AwayFromZero;
        var k = decimal.Round(Math.Min(zahlungsbetrag, kosten), 2, r);
        var rest1 = zahlungsbetrag - k;
        var z = decimal.Round(Math.Min(rest1, zinsen), 2, r);
        var rest2 = rest1 - z;
        var h = decimal.Round(rest2, 2, r);
        var neuerSaldo = decimal.Round(gesamt - zahlungsbetrag, 2, r);
        return new TilgungsAllocation(k, z, h, neuerSaldo);
    }
}

public class VergleichHauptforderungZuerstStrategie : ITilgungsVerrechnungsStrategie
{
    public TilgungsAllocation Verrechne(decimal zahlungsbetrag, decimal kosten, decimal zinsen, decimal hauptforderung)
    {
        if (zahlungsbetrag <= 0m)
            throw new DomainException("Payment must be positive.");
        var gesamt = kosten + zinsen + hauptforderung;
        if (zahlungsbetrag > gesamt)
            throw new DomainException("Payment exceeds outstanding balance.");

        var r = MidpointRounding.AwayFromZero;
        var h = decimal.Round(Math.Min(zahlungsbetrag, hauptforderung), 2, r);
        var rest1 = zahlungsbetrag - h;
        var z = decimal.Round(Math.Min(rest1, zinsen), 2, r);
        var rest2 = rest1 - z;
        var k = decimal.Round(rest2, 2, r);
        var neuerSaldo = decimal.Round(gesamt - zahlungsbetrag, 2, r);
        return new TilgungsAllocation(k, z, h, neuerSaldo);
    }
}
