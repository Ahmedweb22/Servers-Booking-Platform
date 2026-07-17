using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Shtbly.Models;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Localization;

namespace Shtbly.Services.Receipt
{
    public class ReceiptService : IReceiptService
    {
        private readonly IWebHostEnvironment _env;

        public ReceiptService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> GenerateReceiptPdfAsync(Booking booking)
        {
            // Ensure QuestPDF license is set (this should ideally be in Program.cs, but putting it here ensures it runs before generation)
            QuestPDF.Settings.License = LicenseType.Community;

            var receiptsDir = Path.Combine(_env.WebRootPath, "receipts");
            if (!Directory.Exists(receiptsDir))
            {
                Directory.CreateDirectory(receiptsDir);
            }

            var fileName = $"Receipt-{booking.Id}.pdf";
            var filePath = Path.Combine(receiptsDir, fileName);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                    page.Header().Element(header => ComposeHeader(header, booking));
                    page.Content().Element(content => ComposeContent(content, booking));
                    page.Footer().Element(ComposeFooter);
                });
            });

            document.GeneratePdf(filePath);

            return filePath;
        }

        private void ComposeHeader(IContainer container, Booking booking)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("SHTBLY").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text("Payment Receipt").FontSize(14).FontColor(Colors.Grey.Medium);
                });

                row.RelativeItem().AlignRight().Column(column =>
                {
                    column.Item().Text($"Receipt #: {booking.Id}").FontSize(12).SemiBold();
                    column.Item().Text($"Date: {booking.Payment?.PaidAt?.ToString("MMM dd, yyyy") ?? booking.CreatedAt.ToString("MMM dd, yyyy")}").FontSize(12);
                });
            });
        }

        private void ComposeContent(IContainer container, Booking booking)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Billed To:").SemiBold();
                        col.Item().Text($"{booking.Client?.FName ?? "Client"} {booking.Client?.LName ?? ""}".Trim());
                        col.Item().Text(booking.Client?.Email ?? "N/A");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Service Provider:").SemiBold();
                        col.Item().Text($"{booking.Worker?.User?.FName ?? "Worker"} {booking.Worker?.User?.LName ?? ""}".Trim());
                        col.Item().Text(booking.Worker?.User?.Email ?? "N/A");
                    });
                });

                column.Item().PaddingVertical(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Description").SemiBold();
                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).AlignRight().Text("Duration").SemiBold();
                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).AlignRight().Text("Amount").SemiBold();
                    });

                    table.Cell().PaddingTop(10).Text($"Service Booking #{booking.Id}");
                    table.Cell().AlignRight().PaddingTop(10).Text($"{booking.DurationHours} hrs");
                    table.Cell().AlignRight().PaddingTop(10).Text($"{(booking.TotalPrice + booking.DiscountAmt):C}");
                });

                column.Item().PaddingTop(20).AlignRight().Column(col =>
                {
                    if (booking.DiscountAmt > 0)
                    {
                        col.Item().Text($"Discount: -{booking.DiscountAmt:C}").FontColor(Colors.Red.Medium);
                    }
                    col.Item().Text($"Total Paid: {booking.TotalPrice:C}").FontSize(14).SemiBold();
                    
                    if (booking.Payment != null)
                    {
                        col.Item().PaddingTop(10).Text($"Payment Method: {booking.Payment.Method}").FontSize(10).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Transaction ID: {booking.Payment.TransactionId ?? booking.Payment.GatewayRef ?? "N/A"}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    }
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
        }
    }
}
