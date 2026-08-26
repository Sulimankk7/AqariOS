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
    // ─── BRAND COLOR PALETTE (AqariOS Visual Identity) ──────────────────────────
    private const string PrimaryGreen = "#333D29";       // Primary Dark Green (Headings, key borders, brand treatment)
    private const string SecondaryGreen = "#414833";     // Mid Architectural Green
    private const string OliveGreen = "#656D4A";         // Light Olive Green (Logo bar)
    private const string WarmBeige = "#B6AD90";          // Warm Sand / Beige
    private const string AccentBrown = "#582F0E";        // Restrained Brown Accent (Highlight tags / references)
    private const string LogoAmber = "#936639";          // Warm Amber Dot

    // Neutrals & Surfaces
    private const string SurfaceHeroBg = "#F6F4EE";      // Light warm sand for Amount Hero Card
    private const string SurfaceCardBg = "#FAFAF7";      // Soft warm off-white for Info Grid Card
    private const string SurfaceAckBg = "#F7F5EE";       // Subtle warm beige for Acknowledgment
    private const string SurfaceTagBg = "#EDE9DF";       // Warm neutral for badges
    private const string BorderSubtle = "#E5E0D4";       // Soft warm border
    private const string BorderHero = "#D6CFC1";         // Medium warm border for amount card
    private const string BorderDivider = "#EFECE4";      // Very light row divider

    // Typography Colors
    private const string TextPrimary = "#20261A";        // Deep warm near-black for high-contrast readability
    private const string TextMuted = "#736C60";          // Warm muted gray for field labels
    private const string TextDarkMuted = "#4E483E";      // Warm medium gray for legal/body copy
    private const string StatusApprovedBg = "#E9EFE6";   // Muted green-sand status background
    private const string StatusApprovedText = "#24321A"; // Dark forest status text
    private const string StatusApprovedBorder = "#C4D4BD";

    public QuestPdfReceiptGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateReceiptPdfAsync(RentPayment rentPayment, ReceiptPdfModel? model, CancellationToken cancellationToken)
    {
        // Build robust model fallback if model was not explicitly passed
        var m = model ?? new ReceiptPdfModel(
            ReceiptNumber: rentPayment.ReceiptNumber ?? "REC-PENDING",
            IssueDate: rentPayment.UpdatedAt,
            AmountPaid: rentPayment.AmountPaid > 0 ? rentPayment.AmountPaid : rentPayment.AmountDue,
            Currency: string.IsNullOrWhiteSpace(rentPayment.Currency) ? "JOD" : rentPayment.Currency,
            PaymentMethod: rentPayment.PaymentMethod?.ToString() ?? "Cash",
            ReferenceNumber: rentPayment.PaymentReferenceNumber,
            PaymentPurpose: rentPayment.PaymentPurpose.ToString(),
            BillingPeriod: rentPayment.BillingPeriodStart.HasValue && rentPayment.BillingPeriodEnd.HasValue
                ? $"{rentPayment.BillingPeriodStart.Value:dd/MM/yyyy} - {rentPayment.BillingPeriodEnd.Value:dd/MM/yyyy}"
                : null,
            DueDate: rentPayment.DueDate?.ToString("dd/MM/yyyy"),
            DueDateStatus: rentPayment.DueDateStatus.ToString(),
            TenantName: "Tenant",
            TenantPhone: null,
            PropertyName: "Property",
            UnitNumber: "Unit",
            ContractNumber: "Contract"
        );

        var document = Document.Create(container =>
        {
            // PAGE 1 — ARABIC (PRIMARY / OFFICIAL VERSION - RTL)
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9.5f).FontColor(TextPrimary));
                page.ContentFromRightToLeft(); // RTL Layout for Arabic

                page.Header().Element(c => ComposeArabicHeader(c, m));
                page.Content().Element(c => ComposeArabicContent(c, m));
                page.Footer().Element(ComposeArabicFooter);
            });

            // PAGE 2 — ENGLISH (SUMMARY VERSION - LTR)
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9.5f).FontColor(TextPrimary));
                page.ContentFromLeftToRight(); // LTR Layout for English

                page.Header().Element(c => ComposeEnglishHeader(c, m));
                page.Content().Element(c => ComposeEnglishContent(c, m));
                page.Footer().Element(ComposeEnglishFooter);
            });
        });

        var pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }

    public Task<byte[]> GenerateSettlementStatementPdfAsync(SettlementStatementPdfModel model, CancellationToken cancellationToken)
    {
        var document = Document.Create(container =>
        {
            // PAGE 1 — ARABIC (PRIMARY / OFFICIAL VERSION - RTL)
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9.5f).FontColor(TextPrimary));
                page.ContentFromRightToLeft();

                page.Header().Element(c => ComposeSettlementArabicHeader(c, model));
                page.Content().Element(c => ComposeSettlementArabicContent(c, model));
                page.Footer().Element(ComposeArabicFooter);
            });

            // PAGE 2 — ENGLISH (LTR)
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9.5f).FontColor(TextPrimary));
                page.ContentFromLeftToRight();

                page.Header().Element(c => ComposeSettlementEnglishHeader(c, model));
                page.Content().Element(c => ComposeSettlementEnglishContent(c, model));
                page.Footer().Element(ComposeEnglishFooter);
            });
        });

        var pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }

    // ─── BRAND LOGO MARK ────────────────────────────────────────────────────────

    private static void ComposeLogoMark(IContainer container)
    {
        container.Row(row =>
        {
            row.Spacing(2.5f);
            row.AutoItem().Width(4.5f).Height(18).Background(PrimaryGreen);
            row.AutoItem().Width(4.5f).Height(18).Background(OliveGreen);
            row.AutoItem().Width(4.5f).Height(18).Background(WarmBeige);
            row.AutoItem().PaddingTop(13.5f).Width(4.5f).Height(4.5f).Background(LogoAmber);
        });
    }

    private static void ComposeArabicHeader(IContainer container, ReceiptPdfModel m)
    {
        container.BorderBottom(1.5f).BorderColor(PrimaryGreen).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Row(logoRow =>
            {
                logoRow.Spacing(8);
                logoRow.AutoItem().Element(ComposeLogoMark);
                logoRow.RelativeItem().Column(col =>
                {
                    col.Item().Text("عقاري نوت | AqariOS").FontSize(13).Bold().FontColor(PrimaryGreen);
                    col.Item().PaddingTop(1).Text("سند قبض مالي رسمي | Official Payment Receipt").FontSize(8.5f).FontColor(TextMuted);
                });
            });

            row.RelativeItem().AlignLeft().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.Spacing(4);
                    r.AutoItem().Text("رقم السند: ").FontSize(10.5f).Bold().FontColor(PrimaryGreen);
                    r.AutoItem().Text(m.ReceiptNumber).FontSize(10.5f).Bold().FontColor(AccentBrown);
                });

                col.Item().PaddingTop(1).Text($"تاريخ الإصدار: {m.IssueDate:yyyy/MM/dd}").FontSize(8.5f).FontColor(TextMuted);
            });
        });
    }

    private static void ComposeArabicContent(IContainer container, ReceiptPdfModel m)
    {
        container.PaddingVertical(12).Column(col =>
        {
            col.Spacing(10);

            // 1. Highlight Amount Hero Card
            col.Item().Background(SurfaceHeroBg).Border(1).BorderColor(BorderHero).Padding(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("المبلغ المقبوض").FontSize(9).FontColor(SecondaryGreen);
                    c.Item().PaddingTop(1).Text($"{m.AmountPaid:N2} {FormatCurrencyArabic(m.Currency)}").FontSize(21).Bold().FontColor(PrimaryGreen);
                });

                row.RelativeItem().AlignLeft().AlignMiddle().Column(c =>
                {
                    c.Item().Text("حالة الدفع").FontSize(8.5f).FontColor(TextMuted);
                    c.Item().PaddingTop(3).Background(StatusApprovedBg).Border(1).BorderColor(StatusApprovedBorder).PaddingHorizontal(8).PaddingVertical(3).Text(FormatStatusArabic(m.DueDateStatus)).FontSize(9.5f).Bold().FontColor(StatusApprovedText);
                });
            });

            // 1.1 Financial Progress Breakdown (if installment total exists)
            if (m.InstallmentTotal.HasValue && m.InstallmentTotal.Value > 0)
            {
                col.Item().Background(SurfaceCardBg).Border(1).BorderColor(BorderSubtle).Padding(10).Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("إجمالي القسط").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text($"{m.InstallmentTotal.Value:N2} {FormatCurrencyArabic(m.Currency)}").FontSize(9f).Bold().FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("المدفوع في هذه الحركة").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text($"{m.AmountPaid:N2} {FormatCurrencyArabic(m.Currency)}").FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("المدفوع سابقاً").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text($"{(m.PreviouslyPaid ?? 0m):N2} {FormatCurrencyArabic(m.Currency)}").FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("المتبقي بعد هذه الدفعة").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text($"{(m.RemainingAfter ?? 0m):N2} {FormatCurrencyArabic(m.Currency)}").FontSize(9f).Bold().FontColor((m.RemainingAfter ?? 0m) == 0 ? PrimaryGreen : AccentBrown);
                    });
                });
            }

            // 2. Information Details Grid (2-Column Structured Layout)
            col.Item().Background(SurfaceCardBg).Border(1).BorderColor(BorderSubtle).Padding(12).Column(grid =>
            {
                grid.Spacing(8);

                grid.Item().Row(r =>
                {
                    r.Spacing(6);
                    r.AutoItem().Width(3).Height(14).Background(PrimaryGreen);
                    r.RelativeItem().Text("تفاصيل الدفعة والعقد").FontSize(10.5f).Bold().FontColor(PrimaryGreen);
                });

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("اسم المستأجر").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.TenantName).FontSize(9f).Bold().FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("رقم الهاتف").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.TenantPhone ?? "-").FontSize(9f).FontColor(TextPrimary);
                    });
                });

                grid.Item().LineHorizontal(0.5f).LineColor(BorderDivider);

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("العقار / المبنى").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.PropertyName).FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("رقم الشقة / الوحدة").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.UnitNumber).FontSize(9f).FontColor(TextPrimary);
                    });
                });

                grid.Item().LineHorizontal(0.5f).LineColor(BorderDivider);

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("رقم عقد الإيجار").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.ContractNumber).FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("طريقة الدفع").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(2).Row(pr =>
                        {
                            pr.AutoItem().Background(SurfaceTagBg).Border(0.5f).BorderColor(BorderSubtle).PaddingHorizontal(6).PaddingVertical(2).Text(FormatPaymentMethodArabic(m.PaymentMethod)).FontSize(8f).Bold().FontColor(PrimaryGreen);
                        });
                    });
                });

                grid.Item().LineHorizontal(0.5f).LineColor(BorderDivider);

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("سبب الدفع / القسط").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(FormatPurposeArabic(m.PaymentPurpose)).FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("تاريخ الاستحقاق").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.DueDate ?? "-").FontSize(9f).FontColor(TextPrimary);
                    });
                });

                grid.Item().LineHorizontal(0.5f).LineColor(BorderDivider);

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("فترة الاستحقاق").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.BillingPeriod ?? "-").FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("الرقم المرجعي").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.ReferenceNumber ?? "-").FontSize(9f).FontColor(AccentBrown);
                    });
                });

                if (!string.IsNullOrWhiteSpace(m.ChequeNumber) || !string.IsNullOrWhiteSpace(m.BankName))
                {
                    grid.Item().PaddingTop(4).LineHorizontal(1).LineColor(BorderSubtle);

                    grid.Item().Row(r =>
                    {
                        r.Spacing(6);
                        r.AutoItem().Width(3).Height(12).Background(AccentBrown);
                        r.RelativeItem().Text("بيانات الشيك البنكي").FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });

                    grid.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("رقم الشيك").FontSize(8).Bold().FontColor(TextMuted);
                            c.Item().PaddingTop(1).Text(m.ChequeNumber ?? "-").FontSize(9f).FontColor(TextPrimary);
                        });

                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("البنك").FontSize(8).Bold().FontColor(TextMuted);
                            c.Item().PaddingTop(1).Text(m.BankName ?? "-").FontSize(9f).FontColor(TextPrimary);
                        });
                    });

                    grid.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("تاريخ إصدار الشيك").FontSize(8).Bold().FontColor(TextMuted);
                            c.Item().PaddingTop(1).Text(m.ChequeIssueDate ?? "-").FontSize(9f).FontColor(TextPrimary);
                        });

                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("تاريخ استحقاق الشيك").FontSize(8).Bold().FontColor(TextMuted);
                            c.Item().PaddingTop(1).Text(m.ChequeDueDate ?? "-").FontSize(9f).FontColor(TextPrimary);
                        });
                    });
                }
            });

            // 3. Official Acknowledgment Block
            col.Item().Background(SurfaceAckBg).Border(1).BorderColor(BorderSubtle).BorderRight(3.5f).BorderColor(PrimaryGreen).Padding(10).Column(c =>
            {
                c.Item().Text("إقرار واستلام").FontSize(9f).Bold().FontColor(PrimaryGreen);
                c.Item().PaddingTop(3).Text("تم استلام المبلغ المذكور أعلاه بنجاح وقيد حسابه في السجلات المالية لإدارة العقارات الإلكترونية. يعتبر هذا السند إثباتاً رسمياً للوفاء بالحركة المالية المحددة.").FontSize(8f).FontColor(TextDarkMuted).LineHeight(1.35f);
            });
        });
    }

    private static void ComposeArabicFooter(IContainer container)
    {
        container.BorderTop(0.75f).BorderColor(BorderSubtle).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text("هذا المستند صادر إلكترونياً عن نظام عقاري نوت لإدارة العقارات.").FontSize(7.5f).FontColor(TextMuted);
            row.RelativeItem().AlignLeft().Text("الصفحة 1 من 2 (النسخة الرسمية العربية)").FontSize(7.5f).FontColor(TextMuted);
        });
    }

    private static void ComposeEnglishHeader(IContainer container, ReceiptPdfModel m)
    {
        container.BorderBottom(1.5f).BorderColor(PrimaryGreen).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Row(logoRow =>
            {
                logoRow.Spacing(8);
                logoRow.AutoItem().Element(ComposeLogoMark);
                logoRow.RelativeItem().Column(col =>
                {
                    col.Item().Text("AqariOS Property Management").FontSize(13).Bold().FontColor(PrimaryGreen);
                    col.Item().PaddingTop(1).Text("Official Payment Receipt").FontSize(8.5f).FontColor(TextMuted);
                });
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.Spacing(4);
                    r.AutoItem().Text("Receipt No: ").FontSize(10.5f).Bold().FontColor(PrimaryGreen);
                    r.AutoItem().Text(m.ReceiptNumber).FontSize(10.5f).Bold().FontColor(AccentBrown);
                });

                col.Item().PaddingTop(1).Text($"Issue Date: {m.IssueDate:dd/MM/yyyy}").FontSize(8.5f).FontColor(TextMuted);
            });
        });
    }

    private static void ComposeEnglishContent(IContainer container, ReceiptPdfModel m)
    {
        container.PaddingVertical(12).Column(col =>
        {
            col.Spacing(10);

            // 1. Highlight Amount Hero Card
            col.Item().Background(SurfaceHeroBg).Border(1).BorderColor(BorderHero).Padding(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Amount Received").FontSize(9).FontColor(SecondaryGreen);
                    c.Item().PaddingTop(1).Text($"{m.AmountPaid:N2} {m.Currency}").FontSize(21).Bold().FontColor(PrimaryGreen);
                });

                row.RelativeItem().AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().Text("Payment Status").FontSize(8.5f).FontColor(TextMuted);
                    c.Item().PaddingTop(3).Background(StatusApprovedBg).Border(1).BorderColor(StatusApprovedBorder).PaddingHorizontal(8).PaddingVertical(3).Text(m.DueDateStatus).FontSize(9.5f).Bold().FontColor(StatusApprovedText);
                });
            });

            // 1.1 Financial Progress Breakdown
            if (m.InstallmentTotal.HasValue && m.InstallmentTotal.Value > 0)
            {
                col.Item().Background(SurfaceCardBg).Border(1).BorderColor(BorderSubtle).Padding(10).Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Installment Total").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text($"{m.InstallmentTotal.Value:N2} {m.Currency}").FontSize(9f).Bold().FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Transaction Amount").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text($"{m.AmountPaid:N2} {m.Currency}").FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Previously Paid").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text($"{(m.PreviouslyPaid ?? 0m):N2} {m.Currency}").FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Remaining Balance").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text($"{(m.RemainingAfter ?? 0m):N2} {m.Currency}").FontSize(9f).Bold().FontColor((m.RemainingAfter ?? 0m) == 0 ? PrimaryGreen : AccentBrown);
                    });
                });
            }

            // 2. Information Details Grid
            col.Item().Background(SurfaceCardBg).Border(1).BorderColor(BorderSubtle).Padding(12).Column(grid =>
            {
                grid.Spacing(8);

                grid.Item().Row(r =>
                {
                    r.Spacing(6);
                    r.AutoItem().Width(3).Height(14).Background(PrimaryGreen);
                    r.RelativeItem().Text("Payment & Contract Details").FontSize(10.5f).Bold().FontColor(PrimaryGreen);
                });

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Tenant Name").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.TenantName).FontSize(9f).Bold().FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Phone Number").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.TenantPhone ?? "-").FontSize(9f).FontColor(TextPrimary);
                    });
                });

                grid.Item().LineHorizontal(0.5f).LineColor(BorderDivider);

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Property / Building").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.PropertyName).FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Apartment / Unit").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.UnitNumber).FontSize(9f).FontColor(TextPrimary);
                    });
                });

                grid.Item().LineHorizontal(0.5f).LineColor(BorderDivider);

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Contract Number").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.ContractNumber).FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Payment Method").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(2).Row(pr =>
                        {
                            pr.AutoItem().Background(SurfaceTagBg).Border(0.5f).BorderColor(BorderSubtle).PaddingHorizontal(6).PaddingVertical(2).Text(m.PaymentMethod).FontSize(8f).Bold().FontColor(PrimaryGreen);
                        });
                    });
                });

                grid.Item().LineHorizontal(0.5f).LineColor(BorderDivider);

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Payment Purpose").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(FormatPurposeEnglish(m.PaymentPurpose)).FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Due Date").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.DueDate ?? "-").FontSize(9f).FontColor(TextPrimary);
                    });
                });

                grid.Item().LineHorizontal(0.5f).LineColor(BorderDivider);

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Billing Period").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.BillingPeriod ?? "-").FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Reference Number").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.ReferenceNumber ?? "-").FontSize(9f).FontColor(AccentBrown);
                    });
                });

                if (!string.IsNullOrWhiteSpace(m.ChequeNumber) || !string.IsNullOrWhiteSpace(m.BankName))
                {
                    grid.Item().PaddingTop(4).LineHorizontal(1).LineColor(BorderSubtle);

                    grid.Item().Row(r =>
                    {
                        r.Spacing(6);
                        r.AutoItem().Width(3).Height(12).Background(AccentBrown);
                        r.RelativeItem().Text("Cheque Details").FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });

                    grid.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Cheque Number").FontSize(8).Bold().FontColor(TextMuted);
                            c.Item().PaddingTop(1).Text(m.ChequeNumber ?? "-").FontSize(9f).FontColor(TextPrimary);
                        });

                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Bank Name").FontSize(8).Bold().FontColor(TextMuted);
                            c.Item().PaddingTop(1).Text(m.BankName ?? "-").FontSize(9f).FontColor(TextPrimary);
                        });
                    });

                    grid.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Cheque Issue Date").FontSize(8).Bold().FontColor(TextMuted);
                            c.Item().PaddingTop(1).Text(m.ChequeIssueDate ?? "-").FontSize(9f).FontColor(TextPrimary);
                        });

                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Cheque Due Date").FontSize(8).Bold().FontColor(TextMuted);
                            c.Item().PaddingTop(1).Text(m.ChequeDueDate ?? "-").FontSize(9f).FontColor(TextPrimary);
                        });
                    });
                }
            });

            // 3. Official Acknowledgment Block
            col.Item().Background(SurfaceAckBg).Border(1).BorderColor(BorderSubtle).BorderLeft(3.5f).BorderColor(PrimaryGreen).Padding(10).Column(c =>
            {
                c.Item().Text("Payment Confirmation").FontSize(9f).Bold().FontColor(PrimaryGreen);
                c.Item().PaddingTop(3).Text("The above payment has been processed and credited to the property management ledger. This document serves as official proof of payment for this transaction.").FontSize(8f).FontColor(TextDarkMuted).LineHeight(1.35f);
            });
        });
    }

    private static void ComposeEnglishFooter(IContainer container)
    {
        container.BorderTop(0.75f).BorderColor(BorderSubtle).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text("Electronically generated by AqariOS Property Management System.").FontSize(7.5f).FontColor(TextMuted);
            row.RelativeItem().AlignRight().Text("Page 2 of 2 (English Version)").FontSize(7.5f).FontColor(TextMuted);
        });
    }

    // ─── FINAL SETTLEMENT STATEMENT COMPOSITION ───────────────────────────────

    private static void ComposeSettlementArabicHeader(IContainer container, SettlementStatementPdfModel m)
    {
        container.BorderBottom(1.5f).BorderColor(PrimaryGreen).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Row(logoRow =>
            {
                logoRow.Spacing(8);
                logoRow.AutoItem().Element(ComposeLogoMark);
                logoRow.RelativeItem().Column(col =>
                {
                    col.Item().Text("عقاري نوت | AqariOS").FontSize(13).Bold().FontColor(PrimaryGreen);
                    col.Item().PaddingTop(1).Text("سند تسوية القسط النهائي | Final Installment Settlement Statement").FontSize(8.5f).FontColor(TextMuted);
                });
            });

            row.RelativeItem().AlignLeft().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.Spacing(4);
                    r.AutoItem().Text("رقم سند التسوية: ").FontSize(10.5f).Bold().FontColor(PrimaryGreen);
                    r.AutoItem().Text(m.StatementNumber).FontSize(10.5f).Bold().FontColor(AccentBrown);
                });

                col.Item().PaddingTop(1).Text($"تاريخ التسوية: {m.StatementDate:yyyy/MM/dd}").FontSize(8.5f).FontColor(TextMuted);
            });
        });
    }

    private static void ComposeSettlementArabicContent(IContainer container, SettlementStatementPdfModel m)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Spacing(10);

            // 1. Settlement Hero Financial Summary (3-Box Row)
            col.Item().Row(r =>
            {
                r.Spacing(8);

                r.RelativeItem().Background(SurfaceHeroBg).Border(1).BorderColor(BorderHero).Padding(10).Column(c =>
                {
                    c.Item().Text("إجمالي القسط المستحق").FontSize(8).Bold().FontColor(TextMuted);
                    c.Item().PaddingTop(2).Text($"{m.TotalAmountDue:N2} {FormatCurrencyArabic(m.Currency)}").FontSize(14).Bold().FontColor(TextPrimary);
                });

                r.RelativeItem().Background(SurfaceHeroBg).Border(1).BorderColor(BorderHero).Padding(10).Column(c =>
                {
                    c.Item().Text("إجمالي المبلغ المسدد").FontSize(8).Bold().FontColor(SecondaryGreen);
                    c.Item().PaddingTop(2).Text($"{m.TotalAmountPaid:N2} {FormatCurrencyArabic(m.Currency)}").FontSize(14).Bold().FontColor(PrimaryGreen);
                });

                r.RelativeItem().Background(SurfaceHeroBg).Border(1).BorderColor(BorderHero).Padding(10).Column(c =>
                {
                    c.Item().Text("الرصيد المتبقي").FontSize(8).Bold().FontColor(TextMuted);
                    c.Item().PaddingTop(2).Text($"{m.RemainingBalance:N2} {FormatCurrencyArabic(m.Currency)}").FontSize(14).Bold().FontColor(m.RemainingBalance == 0 ? PrimaryGreen : AccentBrown);
                });
            });

            // 2. Installment & Contract Context Box
            col.Item().Background(SurfaceCardBg).Border(1).BorderColor(BorderSubtle).Padding(10).Column(grid =>
            {
                grid.Spacing(6);

                grid.Item().Row(r =>
                {
                    r.Spacing(6);
                    r.AutoItem().Width(3).Height(14).Background(PrimaryGreen);
                    r.RelativeItem().Text("بيانات القسط والوحدة").FontSize(10.5f).Bold().FontColor(PrimaryGreen);
                });

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("اسم المستأجر").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.TenantName).FontSize(9f).Bold().FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("رقم عقد الإيجار").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.ContractNumber).FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("العقار / المبنى").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.PropertyName).FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("رقم الوحدة").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.UnitNumber).FontSize(9f).FontColor(TextPrimary);
                    });
                });

                grid.Item().LineHorizontal(0.5f).LineColor(BorderDivider);

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("تاريخ الاستحقاق").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.DueDate ?? "-").FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("فترة الاستحقاق").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.BillingPeriod ?? "-").FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("حالة القسط").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(FormatStatusArabic(m.Status)).FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });
                });
            });

            // 3. Transactions Table (جدول حركات الدفع)
            col.Item().Column(tableCol =>
            {
                tableCol.Spacing(4);

                tableCol.Item().Row(r =>
                {
                    r.Spacing(6);
                    r.AutoItem().Width(3).Height(14).Background(PrimaryGreen);
                    r.RelativeItem().Text($"حركات الدفع المسجلة ({m.Transactions.Count})").FontSize(10.5f).Bold().FontColor(PrimaryGreen);
                });

                tableCol.Item().Border(1).BorderColor(BorderSubtle).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(2);
                    });

                    // Table Header
                    table.Header(header =>
                    {
                        header.Cell().Background(SurfaceHeroBg).Padding(6).Text("#").FontSize(8).Bold().FontColor(PrimaryGreen);
                        header.Cell().Background(SurfaceHeroBg).Padding(6).Text("تاريخ الحركة").FontSize(8).Bold().FontColor(PrimaryGreen);
                        header.Cell().Background(SurfaceHeroBg).Padding(6).Text("طريقة الدفع").FontSize(8).Bold().FontColor(PrimaryGreen);
                        header.Cell().Background(SurfaceHeroBg).Padding(6).Text("الرقم المرجعي").FontSize(8).Bold().FontColor(PrimaryGreen);
                        header.Cell().Background(SurfaceHeroBg).Padding(6).Text("رقم سند القبض").FontSize(8).Bold().FontColor(PrimaryGreen);
                        header.Cell().Background(SurfaceHeroBg).Padding(6).AlignLeft().Text("المبلغ").FontSize(8).Bold().FontColor(PrimaryGreen);
                    });

                    // Table Rows
                    foreach (var tx in m.Transactions)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).Text(tx.Index.ToString()).FontSize(8.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).Text($"{tx.PaymentDate:yyyy/MM/dd}").FontSize(8.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).Text(FormatPaymentMethodArabic(tx.PaymentMethod)).FontSize(8.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).Text(tx.ReferenceNumber ?? "-").FontSize(8.5f).FontColor(AccentBrown);
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).Text(tx.ReceiptNumber).FontSize(8.5f).Bold().FontColor(PrimaryGreen);
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).AlignLeft().Text($"{tx.Amount:N2} {FormatCurrencyArabic(m.Currency)}").FontSize(8.5f).Bold().FontColor(PrimaryGreen);
                    }

                    // Table Footer / Total Row
                    table.Footer(footer =>
                    {
                        footer.Cell().ColumnSpan(5).Background(SurfaceHeroBg).Padding(6).Text("المجموع الكلي المسدد:").FontSize(8.5f).Bold().FontColor(PrimaryGreen);
                        footer.Cell().Background(SurfaceHeroBg).Padding(6).AlignLeft().Text($"{m.TotalAmountPaid:N2} {FormatCurrencyArabic(m.Currency)}").FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });
                });
            });

            // 4. Official Settlement Acknowledgment
            col.Item().Background(SurfaceAckBg).Border(1).BorderColor(BorderSubtle).BorderRight(3.5f).BorderColor(PrimaryGreen).Padding(10).Column(c =>
            {
                c.Item().Text("إقرار تسوية وإبراء ذمة القسط").FontSize(9.5f).Bold().FontColor(PrimaryGreen);
                c.Item().PaddingTop(3).Text("يشهد هذا السند بأن القسط المالي الموضح أعلاه قد تم سداده بالكامل بموجب حركات الدفع الرسمية المدرجة، ولا يترتب على المستأجر أي رصيد متبقٍ لهذا القسط في سجلات إدارة العقار.").FontSize(8.5f).FontColor(TextDarkMuted).LineHeight(1.35f);
            });
        });
    }

    private static void ComposeSettlementEnglishHeader(IContainer container, SettlementStatementPdfModel m)
    {
        container.BorderBottom(1.5f).BorderColor(PrimaryGreen).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Row(logoRow =>
            {
                logoRow.Spacing(8);
                logoRow.AutoItem().Element(ComposeLogoMark);
                logoRow.RelativeItem().Column(col =>
                {
                    col.Item().Text("AqariOS Property Management").FontSize(13).Bold().FontColor(PrimaryGreen);
                    col.Item().PaddingTop(1).Text("Final Installment Settlement Statement").FontSize(8.5f).FontColor(TextMuted);
                });
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.Spacing(4);
                    r.AutoItem().Text("Statement No: ").FontSize(10.5f).Bold().FontColor(PrimaryGreen);
                    r.AutoItem().Text(m.StatementNumber).FontSize(10.5f).Bold().FontColor(AccentBrown);
                });

                col.Item().PaddingTop(1).Text($"Settlement Date: {m.StatementDate:dd/MM/yyyy}").FontSize(8.5f).FontColor(TextMuted);
            });
        });
    }

    private static void ComposeSettlementEnglishContent(IContainer container, SettlementStatementPdfModel m)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Spacing(10);

            // 1. Settlement Hero Financial Summary (3-Box Row)
            col.Item().Row(r =>
            {
                r.Spacing(8);

                r.RelativeItem().Background(SurfaceHeroBg).Border(1).BorderColor(BorderHero).Padding(10).Column(c =>
                {
                    c.Item().Text("Total Amount Due").FontSize(8).Bold().FontColor(TextMuted);
                    c.Item().PaddingTop(2).Text($"{m.TotalAmountDue:N2} {m.Currency}").FontSize(14).Bold().FontColor(TextPrimary);
                });

                r.RelativeItem().Background(SurfaceHeroBg).Border(1).BorderColor(BorderHero).Padding(10).Column(c =>
                {
                    c.Item().Text("Total Amount Paid").FontSize(8).Bold().FontColor(SecondaryGreen);
                    c.Item().PaddingTop(2).Text($"{m.TotalAmountPaid:N2} {m.Currency}").FontSize(14).Bold().FontColor(PrimaryGreen);
                });

                r.RelativeItem().Background(SurfaceHeroBg).Border(1).BorderColor(BorderHero).Padding(10).Column(c =>
                {
                    c.Item().Text("Remaining Balance").FontSize(8).Bold().FontColor(TextMuted);
                    c.Item().PaddingTop(2).Text($"{m.RemainingBalance:N2} {m.Currency}").FontSize(14).Bold().FontColor(m.RemainingBalance == 0 ? PrimaryGreen : AccentBrown);
                });
            });

            // 2. Installment & Contract Context Box
            col.Item().Background(SurfaceCardBg).Border(1).BorderColor(BorderSubtle).Padding(10).Column(grid =>
            {
                grid.Spacing(6);

                grid.Item().Row(r =>
                {
                    r.Spacing(6);
                    r.AutoItem().Width(3).Height(14).Background(PrimaryGreen);
                    r.RelativeItem().Text("Installment & Property Details").FontSize(10.5f).Bold().FontColor(PrimaryGreen);
                });

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Tenant Name").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.TenantName).FontSize(9f).Bold().FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Contract Number").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.ContractNumber).FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Property / Building").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.PropertyName).FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Unit Number").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.UnitNumber).FontSize(9f).FontColor(TextPrimary);
                    });
                });

                grid.Item().LineHorizontal(0.5f).LineColor(BorderDivider);

                grid.Item().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Due Date").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.DueDate ?? "-").FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Billing Period").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.BillingPeriod ?? "-").FontSize(9f).FontColor(TextPrimary);
                    });

                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Status").FontSize(8).Bold().FontColor(TextMuted);
                        c.Item().PaddingTop(1).Text(m.Status).FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });
                });
            });

            // 3. Transactions Table
            col.Item().Column(tableCol =>
            {
                tableCol.Spacing(4);

                tableCol.Item().Row(r =>
                {
                    r.Spacing(6);
                    r.AutoItem().Width(3).Height(14).Background(PrimaryGreen);
                    r.RelativeItem().Text($"Payment Transactions ({m.Transactions.Count})").FontSize(10.5f).Bold().FontColor(PrimaryGreen);
                });

                tableCol.Item().Border(1).BorderColor(BorderSubtle).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(SurfaceHeroBg).Padding(6).Text("#").FontSize(8).Bold().FontColor(PrimaryGreen);
                        header.Cell().Background(SurfaceHeroBg).Padding(6).Text("Date").FontSize(8).Bold().FontColor(PrimaryGreen);
                        header.Cell().Background(SurfaceHeroBg).Padding(6).Text("Method").FontSize(8).Bold().FontColor(PrimaryGreen);
                        header.Cell().Background(SurfaceHeroBg).Padding(6).Text("Reference").FontSize(8).Bold().FontColor(PrimaryGreen);
                        header.Cell().Background(SurfaceHeroBg).Padding(6).Text("Receipt Number").FontSize(8).Bold().FontColor(PrimaryGreen);
                        header.Cell().Background(SurfaceHeroBg).Padding(6).AlignRight().Text("Amount").FontSize(8).Bold().FontColor(PrimaryGreen);
                    });

                    foreach (var tx in m.Transactions)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).Text(tx.Index.ToString()).FontSize(8.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).Text($"{tx.PaymentDate:dd/MM/yyyy}").FontSize(8.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).Text(tx.PaymentMethod).FontSize(8.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).Text(tx.ReferenceNumber ?? "-").FontSize(8.5f).FontColor(AccentBrown);
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).Text(tx.ReceiptNumber).FontSize(8.5f).Bold().FontColor(PrimaryGreen);
                        table.Cell().BorderBottom(0.5f).BorderColor(BorderDivider).Padding(6).AlignRight().Text($"{tx.Amount:N2} {m.Currency}").FontSize(8.5f).Bold().FontColor(PrimaryGreen);
                    }

                    table.Footer(footer =>
                    {
                        footer.Cell().ColumnSpan(5).Background(SurfaceHeroBg).Padding(6).Text("Total Settled:").FontSize(8.5f).Bold().FontColor(PrimaryGreen);
                        footer.Cell().Background(SurfaceHeroBg).Padding(6).AlignRight().Text($"{m.TotalAmountPaid:N2} {m.Currency}").FontSize(9f).Bold().FontColor(PrimaryGreen);
                    });
                });
            });

            // 4. Official Settlement Acknowledgment
            col.Item().Background(SurfaceAckBg).Border(1).BorderColor(BorderSubtle).BorderLeft(3.5f).BorderColor(PrimaryGreen).Padding(10).Column(c =>
            {
                c.Item().Text("Settlement & Full Clearance Confirmation").FontSize(9.5f).Bold().FontColor(PrimaryGreen);
                c.Item().PaddingTop(3).Text("This document certifies that the rent installment described above has been paid in full via the recorded transactions. No outstanding balance remains for this obligation.").FontSize(8.5f).FontColor(TextDarkMuted).LineHeight(1.35f);
            });
        });
    }

    // ─── TRANSLATION HELPERS ──────────────────────────────────────────────────

    private static string FormatPaymentMethodArabic(string method)
    {
        var lower = method.ToLowerInvariant().Replace("_", "").Replace(" ", "");
        if (lower.Contains("cliq")) return "كليك (CliQ)";
        if (lower.Contains("bank")) return "تحويل بنكي (Bank Transfer)";
        if (lower.Contains("cheque") || lower.Contains("check")) return "شيك (Cheque)";
        if (lower.Contains("efawateer")) return "إي فواتيركم (eFAWATEERCOM)";
        if (lower.Contains("cash")) return "نقداً (Cash)";
        return method;
    }

    private static string FormatCurrencyArabic(string currency)
    {
        if (string.Equals(currency, "JOD", StringComparison.OrdinalIgnoreCase))
            return "دينار أردني";
        return currency;
    }

    private static string FormatStatusArabic(string status)
    {
        var lower = status.ToLowerInvariant().Replace("_", "");
        if (lower.Contains("paid") && !lower.Contains("partially") && !lower.Contains("unpaid")) return "مدفوع بالكامل";
        if (lower.Contains("partially")) return "مدفوع جزئياً";
        if (lower.Contains("pending")) return "قيد المراجعة / معلق";
        if (lower.Contains("overdue") || lower.Contains("late")) return "متأخر / مستحق";
        if (lower.Contains("cancelled")) return "ملغي";
        return status;
    }

    private static string FormatPurposeArabic(string purpose)
    {
        var lower = purpose.ToLowerInvariant();
        if (lower.Contains("installment") || lower.Contains("scheduled")) return "دفعة قسط إيجار شهري";
        if (lower.Contains("unallocated")) return "مبلغ مقبوض إضافي";
        if (lower.Contains("adjustment")) return "تسوية مالية";
        return purpose;
    }

    private static string FormatPurposeEnglish(string purpose)
    {
        var lower = purpose.ToLowerInvariant();
        if (lower.Contains("installment") || lower.Contains("scheduled")) return "Monthly Rent Installment";
        if (lower.Contains("unallocated")) return "Unallocated Receipt";
        if (lower.Contains("adjustment")) return "Financial Adjustment";
        return purpose;
    }
}
