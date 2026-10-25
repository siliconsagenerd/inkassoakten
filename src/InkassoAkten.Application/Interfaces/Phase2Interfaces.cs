using InkassoAkten.Domain.Entities;
using InkassoAkten.Domain.Strategies;
using InkassoAkten.Domain.ValueObjects;

namespace InkassoAkten.Application.Interfaces;

public interface IAkteRepository
{
    Task<InkassoAkte?> GetByIdAsync(int akteId, CancellationToken ct = default);
    Task<IReadOnlyList<InkassoAkte>> GetAllAsync(CancellationToken ct = default);
}

public interface IZahlungRepository
{
    Task AddAsync(Zahlung zahlung, CancellationToken ct = default);
    Task<IReadOnlyList<Zahlung>> GetByAkteAsync(int akteId, CancellationToken ct = default);
}

public interface IBuchungRepository
{
    Task AddAsync(Buchung buchung, CancellationToken ct = default);
    Task<IReadOnlyList<Buchung>> GetByAkteAsync(int akteId, CancellationToken ct = default);
}

public interface IRatenzahlungsplanRepository
{
    Task AddAsync(Ratenzahlungsplan plan, CancellationToken ct = default);
    Task<IReadOnlyList<Ratenzahlungsplan>> GetByAkteAsync(int akteId, CancellationToken ct = default);
}

public interface IBasiszinsRepository
{
    Task<IReadOnlyList<BasiszinsPeriode>> GetAllAsync(CancellationToken ct = default);
}

public interface IZahlungPostingService
{
    Task<Zahlung> PosteZahlungAsync(
        int akteId,
        decimal betrag,
        DateTime zahlungsdatumUtc,
        ITilgungsVerrechnungsStrategie strategie,
        CancellationToken ct = default);
}

public interface IStateTransitionService
{
    Task TransitionAsync(int akteId, Func<InkassoAkte, Task> transition, CancellationToken ct = default);
}
