using Microsoft.EntityFrameworkCore;
using InkassoAkten.Domain.Entities;
using InkassoAkten.Domain.Strategies;
using InkassoAkten.Domain.ValueObjects;
using InkassoAkten.Application.Interfaces;

namespace InkassoAkten.Infrastructure;

public class ZahlungPostingService : IZahlungPostingService
{
    private readonly InkassoAktenDbContext _db;
    public ZahlungPostingService(InkassoAktenDbContext db) => _db = db;

    public async Task<Zahlung> PosteZahlungAsync(int akteId, decimal betrag, DateTime zahlungsdatumUtc, ITilgungsVerrechnungsStrategie strategie, CancellationToken ct = default)
    {
        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var akte = await _db.InkassoAkten.FirstAsync(a => a.AkteId == akteId, ct);
            var gesamt = akte.Gesamtsaldo;
            if (betrag <= 0m) throw new Domain.Exceptions.DomainException("Payment must be positive.");
            if (betrag > gesamt) throw new Domain.Exceptions.DomainException("Payment exceeds outstanding balance.");

            var alloc = strategie.Verrechne(betrag, akte.OffeneKosten, akte.OffeneZinsen, akte.OffeneHauptforderung);

            // update open amounts
            akte.SetzeOffeneBetrage(
                akte.OffeneKosten - alloc.GetilgteKosten,
                akte.OffeneZinsen - alloc.GetilgteZinsen,
                akte.OffeneHauptforderung - alloc.GetilgteHauptforderung);

            var zahlung = new Zahlung
            {
                AkteId = akteId,
                Betrag = betrag,
                ZahlungsdatumUtc = zahlungsdatumUtc,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.Zahlungen.Add(zahlung);
            await _db.SaveChangesAsync(ct);

            var buchung = new Buchung
            {
                ZahlungId = zahlung.ZahlungId,
                AkteId = akteId,
                BetragKosten = alloc.GetilgteKosten,
                BetragZinsen = alloc.GetilgteZinsen,
                BetragHauptforderung = alloc.GetilgteHauptforderung,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.Buchungen.Add(buchung);

            _db.AuditEintraege.Add(new AuditEintrag
            {
                EntitaetName = "Zahlung",
                EntitaetId = zahlung.ZahlungId,
                Aktion = "PaymentPosted",
                Akteur = "system",
                ZeitstempelUtc = DateTime.UtcNow,
                NeuWerteJson = $"{{\"betrag\":{betrag},\"kosten\":{alloc.GetilgteKosten},\"zinsen\":{alloc.GetilgteZinsen},\"haupt\":{alloc.GetilgteHauptforderung}}}"
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return zahlung;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
