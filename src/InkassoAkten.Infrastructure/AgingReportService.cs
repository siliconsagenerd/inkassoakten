using Microsoft.EntityFrameworkCore;
using InkassoAkten.Application.Interfaces;

namespace InkassoAkten.Infrastructure;

public class AgingReportService : IAgingReportService
{
    private readonly InkassoAktenDbContext _db;

    public AgingReportService(InkassoAktenDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ForderungsAltersstrukturRow>> GetAltersstrukturAsync(CancellationToken ct = default)
    {
        // Query the SQL view directly using EF Core SqlQuery
        return await _db.Database
            .SqlQuery<ForderungsAltersstrukturRow>($@"
SELECT
    AkteId,
    Akte,
    Schuldner,
    Mandant,
    Hauptforderung,
    OffeneHauptforderung,
    OffeneZinsen,
    OffeneKosten,
    Gesamtsaldo,
    TageUeberfaellig,
    Altersbucket
FROM vw_ForderungsAltersstruktur
ORDER BY TageUeberfaellig DESC")
            .ToListAsync(ct);
    }
}
