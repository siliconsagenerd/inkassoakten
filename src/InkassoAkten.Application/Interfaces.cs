using InkassoAkten.Domain.Entities;

namespace InkassoAkten.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IInkassoAktenRepository
{
    Task<IReadOnlyList<Mandant>> GetMandantenAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Schuldner>> GetSchuldnerAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InkassoAkte>> GetInkassoAktenAsync(CancellationToken cancellationToken = default);
    Task SeedAsync(CancellationToken cancellationToken = default);
}
