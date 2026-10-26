using Microsoft.EntityFrameworkCore;
using InkassoAkten.Domain.Entities;
using InkassoAkten.Application.Interfaces;

namespace InkassoAkten.Infrastructure;

public class InkassoAktenDbContext : DbContext, IUnitOfWork
{
    public InkassoAktenDbContext(DbContextOptions<InkassoAktenDbContext> options) : base(options) { }

    public DbSet<Mandant> Mandanten => Set<Mandant>();
    public DbSet<Schuldner> Schuldner => Set<Schuldner>();
    public DbSet<Forderung> Forderungen => Set<Forderung>();
    public DbSet<InkassoAkte> InkassoAkten => Set<InkassoAkte>();
    public DbSet<Zahlung> Zahlungen => Set<Zahlung>();
    public DbSet<Buchung> Buchungen => Set<Buchung>();
    public DbSet<Ratenzahlungsplan> Ratenzahlungsplaene => Set<Ratenzahlungsplan>();
    public DbSet<Rate> Raten => Set<Rate>();
    public DbSet<BasiszinsPeriode> BasiszinsPerioden => Set<BasiszinsPeriode>();
    public DbSet<AuditEintrag> AuditEintraege => Set<AuditEintrag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Mandant>(entity =>
        {
            entity.HasKey(e => e.MandantId);
            entity.Property(e => e.Firmenname).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Schuldner>(entity =>
        {
            entity.HasKey(e => e.SchuldnerId);
            entity.Property(e => e.NachnameOderFirma).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Vorname).HasMaxLength(100);
            entity.Property(e => e.Strasse).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PLZ).IsRequired().HasMaxLength(5);
            entity.Property(e => e.Ort).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.NachnameOderFirma, e.PLZ });
        });

        modelBuilder.Entity<Forderung>(entity =>
        {
            entity.HasKey(e => e.ForderungId);
            entity.Property(e => e.Rechnungsnummer).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Hauptforderung).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Waehrung).IsRequired().HasMaxLength(3).HasDefaultValue("EUR");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.Faelligkeitsdatum);
            entity.HasOne(e => e.Mandant).WithMany().HasForeignKey(e => e.MandantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Schuldner).WithMany().HasForeignKey(e => e.SchuldnerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InkassoAkte>(entity =>
        {
            entity.HasKey(e => e.AkteId);
            entity.Property(e => e.Aktenzeichen).IsRequired().HasMaxLength(30);
            entity.HasIndex(e => e.Aktenzeichen).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.OffeneKosten).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OffeneZinsen).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OffeneHauptforderung).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.AkteStatus);
            entity.HasIndex(e => e.MandantId);
            entity.HasIndex(e => e.SchuldnerId);
            entity.HasIndex(e => new { e.MandantId, e.AkteStatus });
            entity.HasOne(e => e.Mandant).WithMany().HasForeignKey(e => e.MandantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Schuldner).WithMany().HasForeignKey(e => e.SchuldnerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Forderung).WithMany().HasForeignKey(e => e.ForderungId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEintrag>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.EntitaetName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Aktion).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Akteur).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AltWerteJson).HasMaxLength(4000);
            entity.Property(e => e.NeuWerteJson).HasMaxLength(4000);
        });

        modelBuilder.Entity<Zahlung>(entity =>
        {
            entity.HasKey(e => e.ZahlungId);
            entity.Property(e => e.Betrag).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.AkteId);
        });

        modelBuilder.Entity<Buchung>(entity =>
        {
            entity.HasKey(e => e.BuchungId);
            entity.Property(e => e.BetragKosten).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BetragZinsen).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BetragHauptforderung).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.AkteId);
            entity.HasIndex(e => e.ZahlungId);
        });

        modelBuilder.Entity<Ratenzahlungsplan>(entity =>
        {
            entity.HasKey(e => e.PlanId);
            entity.Property(e => e.Gesamtbetrag).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.AkteId);
            entity.HasMany(e => e.Raten).WithOne().HasForeignKey(r => r.PlanId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Rate>(entity =>
        {
            entity.HasKey(e => e.RateId);
            entity.Property(e => e.Betrag).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.PlanId);
        });

        modelBuilder.Entity<BasiszinsPeriode>(entity =>
        {
            entity.HasKey(e => e.BasiszinsPeriodeId);
            entity.Property(e => e.Basiszinssatz).HasColumnType("decimal(18,4)");
        });
    }
}
