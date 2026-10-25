using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkassoAkten.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEintraege",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntitaetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntitaetId = table.Column<int>(type: "int", nullable: false),
                    Aktion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Akteur = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ZeitstempelUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AltWerteJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NeuWerteJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEintraege", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "Mandanten",
                columns: table => new
                {
                    MandantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Firmenname = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IstAktiv = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mandanten", x => x.MandantId);
                });

            migrationBuilder.CreateTable(
                name: "Schuldner",
                columns: table => new
                {
                    SchuldnerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchuldnerTyp = table.Column<int>(type: "int", nullable: false),
                    NachnameOderFirma = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Vorname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Strasse = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PLZ = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Ort = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schuldner", x => x.SchuldnerId);
                });

            migrationBuilder.CreateTable(
                name: "Forderungen",
                columns: table => new
                {
                    ForderungId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MandantId = table.Column<int>(type: "int", nullable: false),
                    SchuldnerId = table.Column<int>(type: "int", nullable: false),
                    Rechnungsnummer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Hauptforderung = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Faelligkeitsdatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Waehrung = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forderungen", x => x.ForderungId);
                    table.ForeignKey(
                        name: "FK_Forderungen_Mandanten_MandantId",
                        column: x => x.MandantId,
                        principalTable: "Mandanten",
                        principalColumn: "MandantId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Forderungen_Schuldner_SchuldnerId",
                        column: x => x.SchuldnerId,
                        principalTable: "Schuldner",
                        principalColumn: "SchuldnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InkassoAkten",
                columns: table => new
                {
                    AkteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Aktenzeichen = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MandantId = table.Column<int>(type: "int", nullable: false),
                    SchuldnerId = table.Column<int>(type: "int", nullable: false),
                    ForderungId = table.Column<int>(type: "int", nullable: false),
                    AkteStatus = table.Column<int>(type: "int", nullable: false),
                    VerzugsbeginnDatum = table.Column<DateOnly>(type: "date", nullable: false),
                    ZinsenBerechnetBis = table.Column<DateOnly>(type: "date", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InkassoAkten", x => x.AkteId);
                    table.ForeignKey(
                        name: "FK_InkassoAkten_Forderungen_ForderungId",
                        column: x => x.ForderungId,
                        principalTable: "Forderungen",
                        principalColumn: "ForderungId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InkassoAkten_Mandanten_MandantId",
                        column: x => x.MandantId,
                        principalTable: "Mandanten",
                        principalColumn: "MandantId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InkassoAkten_Schuldner_SchuldnerId",
                        column: x => x.SchuldnerId,
                        principalTable: "Schuldner",
                        principalColumn: "SchuldnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Forderungen_MandantId",
                table: "Forderungen",
                column: "MandantId");

            migrationBuilder.CreateIndex(
                name: "IX_Forderungen_SchuldnerId",
                table: "Forderungen",
                column: "SchuldnerId");

            migrationBuilder.CreateIndex(
                name: "IX_InkassoAkten_Aktenzeichen",
                table: "InkassoAkten",
                column: "Aktenzeichen",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InkassoAkten_AkteStatus",
                table: "InkassoAkten",
                column: "AkteStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InkassoAkten_ForderungId",
                table: "InkassoAkten",
                column: "ForderungId");

            migrationBuilder.CreateIndex(
                name: "IX_InkassoAkten_MandantId",
                table: "InkassoAkten",
                column: "MandantId");

            migrationBuilder.CreateIndex(
                name: "IX_InkassoAkten_SchuldnerId",
                table: "InkassoAkten",
                column: "SchuldnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Schuldner_NachnameOderFirma_PLZ",
                table: "Schuldner",
                columns: new[] { "NachnameOderFirma", "PLZ" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEintraege");

            migrationBuilder.DropTable(
                name: "InkassoAkten");

            migrationBuilder.DropTable(
                name: "Forderungen");

            migrationBuilder.DropTable(
                name: "Mandanten");

            migrationBuilder.DropTable(
                name: "Schuldner");
        }
    }
}
