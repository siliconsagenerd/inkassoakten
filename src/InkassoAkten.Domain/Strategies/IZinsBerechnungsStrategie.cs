using InkassoAkten.Domain.Entities;
using InkassoAkten.Domain.ValueObjects;

namespace InkassoAkten.Domain.Strategies;

public interface IZinsBerechnungsStrategie
{
    ZinsStaffel Berechne(
        decimal kapital,
        DateTime verzugsbeginn,
        DateTime berechnetBis,
        SchuldnerTyp schuldnerTyp,
        IEnumerable<BasiszinsPeriode> basiszinsHistorie);
}
