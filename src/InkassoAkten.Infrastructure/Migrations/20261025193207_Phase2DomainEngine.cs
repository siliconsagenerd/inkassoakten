using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkassoAkten.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2DomainEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Mahnstufe",
                table: "InkassoAkten",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OffeneHauptforderung",
                table: "InkassoAkten",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OffeneKosten",
                table: "InkassoAkten",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OffeneZinsen",
                table: "InkassoAkten",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BasiszinsPerioden",
                columns: table => new
                {
                    BasiszinsPeriodeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GueltigAb = table.Column<DateOnly>(type: "date", nullable: false),
                    Basiszinssatz = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasiszinsPerioden", x => x.BasiszinsPeriodeId);
                });

            migrationBuilder.CreateTable(
                name: "Buchungen",
                columns: table => new
                {
                    BuchungId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZahlungId = table.Column<int>(type: "int", nullable: false),
                    AkteId = table.Column<int>(type: "int", nullable: false),
                    BetragKosten = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BetragZinsen = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BetragHauptforderung = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buchungen", x => x.BuchungId);
                });

            migrationBuilder.CreateTable(
                name: "Ratenzahlungsplaene",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AkteId = table.Column<int>(type: "int", nullable: false),
                    Gesamtbetrag = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IstDefaulted = table.Column<bool>(type: "bit", nullable: false),
                    DefaultedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    InkassoAkteAkteId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratenzahlungsplaene", x => x.PlanId);
                    table.ForeignKey(
                        name: "FK_Ratenzahlungsplaene_InkassoAkten_InkassoAkteAkteId",
                        column: x => x.InkassoAkteAkteId,
                        principalTable: "InkassoAkten",
                        principalColumn: "AkteId");
                });

            migrationBuilder.CreateTable(
                name: "Zahlungen",
                columns: table => new
                {
                    ZahlungId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AkteId = table.Column<int>(type: "int", nullable: false),
                    Betrag = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ZahlungsdatumUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zahlungen", x => x.ZahlungId);
                });

            migrationBuilder.CreateTable(
                name: "Raten",
                columns: table => new
                {
                    RateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    Faelligkeitsdatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Betrag = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IstBezahlt = table.Column<bool>(type: "bit", nullable: false),
                    BezahltAmUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raten", x => x.RateId);
                    table.ForeignKey(
                        name: "FK_Raten_Ratenzahlungsplaene_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Ratenzahlungsplaene",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Buchungen_AkteId",
                table: "Buchungen",
                column: "AkteId");

            migrationBuilder.CreateIndex(
                name: "IX_Buchungen_ZahlungId",
                table: "Buchungen",
                column: "ZahlungId");

            migrationBuilder.CreateIndex(
                name: "IX_Raten_PlanId",
                table: "Raten",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratenzahlungsplaene_AkteId",
                table: "Ratenzahlungsplaene",
                column: "AkteId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratenzahlungsplaene_InkassoAkteAkteId",
                table: "Ratenzahlungsplaene",
                column: "InkassoAkteAkteId");

            migrationBuilder.CreateIndex(
                name: "IX_Zahlungen_AkteId",
                table: "Zahlungen",
                column: "AkteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BasiszinsPerioden");

            migrationBuilder.DropTable(
                name: "Buchungen");

            migrationBuilder.DropTable(
                name: "Raten");

            migrationBuilder.DropTable(
                name: "Zahlungen");

            migrationBuilder.DropTable(
                name: "Ratenzahlungsplaene");

            migrationBuilder.DropColumn(
                name: "Mahnstufe",
                table: "InkassoAkten");

            migrationBuilder.DropColumn(
                name: "OffeneHauptforderung",
                table: "InkassoAkten");

            migrationBuilder.DropColumn(
                name: "OffeneKosten",
                table: "InkassoAkten");

            migrationBuilder.DropColumn(
                name: "OffeneZinsen",
                table: "InkassoAkten");
        }
    }
}
