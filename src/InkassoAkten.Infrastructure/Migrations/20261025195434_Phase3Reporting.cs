using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkassoAkten.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3Reporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        CREATE INDEX IX_InkassoAkten_MandantId_AkteStatus
        ON InkassoAkten (MandantId, AkteStatus);
        ");
            migrationBuilder.Sql(@"
        CREATE INDEX IX_Forderungen_Faelligkeitsdatum
        ON Forderungen (Faelligkeitsdatum);
        ");
            migrationBuilder.Sql(@"
        CREATE VIEW vw_ForderungsAltersstruktur AS
        SELECT
        a.AkteId,
        a.Aktenzeichen AS Akte,
        s.NachnameOderFirma AS Schuldner,
        m.Firmenname AS Mandant,
        f.Hauptforderung AS Hauptforderung,
        a.OffeneHauptforderung AS OffeneHauptforderung,
        a.OffeneZinsen AS OffeneZinsen,
        a.OffeneKosten AS OffeneKosten,
        (a.OffeneKosten + a.OffeneZinsen + a.OffeneHauptforderung) AS Gesamtsaldo,
        DATEDIFF(day, f.Faelligkeitsdatum, CAST(GETUTCDATE() AS date)) AS TageUeberfaellig,
        CASE
            WHEN DATEDIFF(day, f.Faelligkeitsdatum, CAST(GETUTCDATE() AS date)) BETWEEN 1 AND 30 THEN '1-30'
            WHEN DATEDIFF(day, f.Faelligkeitsdatum, CAST(GETUTCDATE() AS date)) BETWEEN 31 AND 60 THEN '31-60'
            WHEN DATEDIFF(day, f.Faelligkeitsdatum, CAST(GETUTCDATE() AS date)) BETWEEN 61 AND 90 THEN '61-90'
            WHEN DATEDIFF(day, f.Faelligkeitsdatum, CAST(GETUTCDATE() AS date)) > 90 THEN '90+'
            ELSE '0 (aktuell)'
        END AS Altersbucket
        FROM InkassoAkten a
        JOIN Forderungen f ON a.ForderungId = f.ForderungId
        JOIN Schuldner s ON a.SchuldnerId = s.SchuldnerId
        JOIN Mandanten m ON a.MandantId = m.MandantId;
        ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ForderungsAltersstruktur;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Forderungen_Faelligkeitsdatum ON Forderungen;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_InkassoAkten_MandantId_AkteStatus ON InkassoAkten;");
        }
    }
}
