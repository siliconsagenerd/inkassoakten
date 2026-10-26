using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InkassoAkten.Application.Interfaces;

namespace InkassoAkten.Infrastructure;

public class PdfStatementService : IPdfStatementService
{
    static PdfStatementService()
    {
        // QuestPDF Community License for open-source / evaluation
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneriereForderungsaufstellung(ForderungsaufstellungData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FORDERUNGSAUFSTELLUNG").Bold().FontSize(18);
                            c.Item().Text($"Aktenzeichen: {data.Aktenzeichen}").FontSize(11).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().Text("TECHNISCHE DEMO").FontColor(Colors.Red.Medium).Bold().FontSize(9);
                            c.Item().Text("Keine Rechtsberatung").FontColor(Colors.Grey.Medium).FontSize(8);
                            c.Item().Text($"Stand: {data.ErstelltAmUtc:yyyy-MM-dd HH:mm} UTC").FontSize(8);
                        });
                    });
                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    // Parties
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("Gläubiger / Mandant").Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(data.MandantName).FontSize(11);
                        });
                        row.ConstantItem(15);
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("Schuldner").Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(data.SchuldnerName).FontSize(11);
                            c.Item().Text(data.SchuldnerAdresse).FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });

                    // Summary Table
                    col.Item().Text("Forderungsübersicht").Bold().FontSize(12);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                        });

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text("Ursprüngliche Hauptforderung");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{data.UrspruenglicheHauptforderung:N2} €");

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text("Offene Hauptforderung");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{data.OffeneHauptforderung:N2} €");

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text("Offene Verzugszinsen (§ 288 BGB)");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{data.OffeneZinsen:N2} €");

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text("Offene Kosten / Gebühren");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"{data.OffeneKosten:N2} €");

                        table.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Gesamtsaldo").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"{data.Gesamtsaldo:N2} €").Bold().FontSize(12);
                    });

                    // Ledger
                    col.Item().Text("Zahlungs- und Buchungshistorie").Bold().FontSize(12);
                    if (data.Buchungen.Count == 0)
                    {
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text("Bisher keine Zahlungseingänge verbucht.").FontColor(Colors.Grey.Darken1).Italic();
                    }
                    else
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(40);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten4).Padding(3).Text("#").Bold();
                                h.Cell().Background(Colors.Grey.Lighten4).Padding(3).Text("Datum").Bold();
                                h.Cell().Background(Colors.Grey.Lighten4).Padding(3).AlignRight().Text("Kosten").Bold();
                                h.Cell().Background(Colors.Grey.Lighten4).Padding(3).AlignRight().Text("Zinsen").Bold();
                                h.Cell().Background(Colors.Grey.Lighten4).Padding(3).AlignRight().Text("Hauptford.").Bold();
                                h.Cell().Background(Colors.Grey.Lighten4).Padding(3).AlignRight().Text("Gesamt").Bold();
                            });

                            foreach (var b in data.Buchungen)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(b.BuchungId.ToString());
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(b.DatumUtc.ToString("dd.MM.yyyy"));
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{b.Kosten:N2} €");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{b.Zinsen:N2} €");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{b.Hauptforderung:N2} €");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{b.Gesamt:N2} €");
                            }
                        });
                    }

                    // Disclaimer note
                    col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(6).Text(
                        "Hinweis: Dieses Dokument wurde automatisiert erstellt. Tilgungsverrechnung erfolgt nach § 367 Abs. 1 BGB " +
                        "(Kosten vor Zinsen vor Hauptforderung). Verzugszinsen gemäß § 288 BGB. Technische Machbarkeitsstudie — kein rechtsverbindlicher Titel."
                    ).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("InkassoAkten · Technische Demonstration").FontSize(8).FontColor(Colors.Grey.Medium);
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Seite ");
                        x.CurrentPageNumber();
                        x.Span(" von ");
                        x.TotalPages();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
