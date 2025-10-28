using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TheOtherSide.Models;

namespace TheOtherSide.Services
{
    public class OrdersPdfService
    {
        public byte[] Build(List<SaleEntry> orders, string username)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);                  
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("TheOtherSide").SemiBold().FontSize(18);
                            col.Item().Text($"Los pedidos de {username}")
                                     .FontSize(12).FontColor(Colors.Grey.Darken2);
                            col.Item().Text($"Generado: {DateTime.Now:g}")
                                     .FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    page.Content().Column(col =>
                    {
                        if (orders == null || orders.Count == 0)
                        {
                            col.Item().PaddingVertical(16).Text("No hay pedidos confirmados.")
                                   .Italic().FontColor(Colors.Grey.Darken1);
                            return;
                        }

                        foreach (var sale in orders.OrderByDescending(x => x.DateUtc))
                        {
                            col.Item().Text($"> Pedido del {sale.DateUtc.ToLocalTime():g}")
                                      .SemiBold().FontSize(12);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(60);   // ID
                                    cols.RelativeColumn(5);    // Producto
                                    cols.ConstantColumn(70);   // Cant.
                                    cols.ConstantColumn(80);   // Precio
                                    cols.ConstantColumn(90);   // Subtotal
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Text("ID").SemiBold();
                                    h.Cell().Text("Producto").SemiBold();
                                    h.Cell().AlignCenter().Text("Cant.").SemiBold();
                                    h.Cell().AlignRight().Text("Precio").SemiBold();
                                    h.Cell().AlignRight().Text("Subtotal").SemiBold();
                                });

                                if (sale.Items != null)
                                {
                                    foreach (var it in sale.Items)
                                    {
                                        table.Cell().Text(it.Id.ToString());
                                        table.Cell().Text(it.Name);
                                        table.Cell().AlignCenter().Text(it.Qty.ToString());
                                        table.Cell().AlignRight().Text($"${it.Price:0.##}");
                                        table.Cell().AlignRight().Text($"${it.Subtotal:0.##}");
                                    }
                                }

                                table.Cell().ColumnSpan(4).AlignRight().Text("Total").SemiBold();
                                table.Cell().AlignRight().Text($"${sale.Total:0.##}").SemiBold();
                            });

                            col.Item().PaddingBottom(10).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();
        }
    }
}
