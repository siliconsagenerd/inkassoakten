using InkassoAkten.Domain.ValueObjects;

namespace InkassoAkten.Domain.Strategies;

public interface ITilgungsVerrechnungsStrategie
{
    TilgungsAllocation Verrechne(decimal zahlungsbetrag, decimal kosten, decimal zinsen, decimal hauptforderung);
}
