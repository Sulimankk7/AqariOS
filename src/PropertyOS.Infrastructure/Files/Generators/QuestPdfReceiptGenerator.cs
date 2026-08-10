using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PropertyOS.Infrastructure.Files.Generators;

public class QuestPdfReceiptGenerator : IReceiptPdfGenerator
{
    public QuestPdfReceiptGenerator()
    {
        // QuestPDF requires setting the license type for commercial/community usage.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateReceiptPdfAsync(RentPayment rentPayment, CancellationToken cancellationToken)
    {
        // In a real application, we would use a more detailed template with 
        // localization, fonts, company logos, etc. This is a minimal implementation.
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x, rentPayment));
                page.Footer().Element(ComposeFooter);
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return Task.FromResult(stream.ToArray());
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("OFFICIAL RECEIPT").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("AqariOS Property Management");
            });

            row.ConstantItem(100).Height(50).Placeholder(); // Logo placeholder
        });
    }

    private void ComposeContent(IContainer container, RentPayment rentPayment)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(5);

            column.Item().Text($"Receipt Number: {rentPayment.ReceiptNumber ?? "N/A"}").SemiBold();
            column.Item().Text($"Payment Date: {rentPayment.UpdatedAt:yyyy-MM-dd}");
            column.Item().Text($"Amount: {rentPayment.AmountPaid} {rentPayment.Currency}").SemiBold();
            column.Item().Text($"Payment Method: {rentPayment.PaymentMethod}");
            
            if (!string.IsNullOrWhiteSpace(rentPayment.PaymentReferenceNumber))
            {
                column.Item().Text($"Reference: {rentPayment.PaymentReferenceNumber}");
            }
            
            column.Item().PaddingTop(25).Text("Thank you for your payment.");
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
