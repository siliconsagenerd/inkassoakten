using Microsoft.EntityFrameworkCore;
using InkassoAkten.Domain.Entities;
using InkassoAkten.Application.Interfaces;

namespace InkassoAkten.Infrastructure;

public class AkteRepository : IAkteRepository
{
    private readonly InkassoAktenDbContext _db;
    public AkteRepository(InkassoAktenDbContext db) => _db = db;
    public Task<InkassoAkte?> GetByIdAsync(int akteId, CancellationToken ct = default) =>
        _db.InkassoAkten.Include(a => a.Ratenzahlungsplaene).ThenInclude(p => p.Raten).FirstOrDefaultAsync(a => a.AkteId == akteId, ct);
    public Task<IReadOnlyList<InkassoAkte>> GetAllAsync(CancellationToken ct = default) =>
        _db.InkassoAkten.ToListAsync(ct).ContinueWith(t => (IReadOnlyList<InkassoAkte>)t.Result, ct);
}

public class ZahlungRepository : IZahlungRepository
{
    private readonly InkassoAktenDbContext _db;
    public ZahlungRepository(InkassoAktenDbContext db) => _db = db;
    public Task AddAsync(Zahlung z, CancellationToken ct = default) { _db.Zahlungen.Add(z); return Task.CompletedTask; }
    public Task<IReadOnlyList<Zahlung>> GetByAkteAsync(int akteId, CancellationToken ct = default) =>
        _db.Zahlungen.Where(z => z.AkteId == akteId).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Zahlung>)t.Result, ct);
}

public class BuchungRepository : IBuchungRepository
{
    private readonly InkassoAktenDbContext _db;
    public BuchungRepository(InkassoAktenDbContext db) => _db = db;
    public Task AddAsync(Buchung b, CancellationToken ct = default) { _db.Buchungen.Add(b); return Task.CompletedTask; }
    public Task<IReadOnlyList<Buchung>> GetByAkteAsync(int akteId, CancellationToken ct = default) =>
        _db.Buchungen.Where(b => b.AkteId == akteId).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Buchung>)t.Result, ct);
}

public class RatenzahlungsplanRepository : IRatenzahlungsplanRepository
{
    private readonly InkassoAktenDbContext _db;
    public RatenzahlungsplanRepository(InkassoAktenDbContext db) => _db = db;
    public Task AddAsync(Ratenzahlungsplan p, CancellationToken ct = default) { _db.Ratenzahlungsplaene.Add(p); return Task.CompletedTask; }
    public Task<IReadOnlyList<Ratenzahlungsplan>> GetByAkteAsync(int akteId, CancellationToken ct = default) =>
        _db.Ratenzahlungsplaene.Include(p => p.Raten).Where(p => p.AkteId == akteId).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Ratenzahlungsplan>)t.Result, ct);
}

public class BasiszinsRepository : IBasiszinsRepository
{
    private readonly InkassoAktenDbContext _db;
    public BasiszinsRepository(InkassoAktenDbContext db) => _db = db;
    public Task<IReadOnlyList<BasiszinsPeriode>> GetAllAsync(CancellationToken ct = default) =>
        _db.BasiszinsPerioden.ToListAsync(ct).ContinueWith(t => (IReadOnlyList<BasiszinsPeriode>)t.Result, ct);
}
