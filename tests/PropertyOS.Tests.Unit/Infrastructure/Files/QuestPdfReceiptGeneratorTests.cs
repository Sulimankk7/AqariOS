using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Files.Generators;
using Xunit;

namespace PropertyOS.Tests.Unit.Infrastructure.Files;

public class QuestPdfReceiptGeneratorTests
{
    private readonly QuestPdfReceiptGenerator _generator;

    public QuestPdfReceiptGeneratorTests()
    {
        _generator = new QuestPdfReceiptGenerator();
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_WithCompleteModel_ProducesValidPdfBytes()
    {
        // Arrange
        var rentPayment = RentPayment.Create(
            companyId: Guid.NewGuid(),
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.ScheduledInstallment,
            amountDue: 400.00m,
            currency: "JOD",
            billingPeriodStart: new DateOnly(2026, 8, 1),
            billingPeriodEnd: new DateOnly(2026, 8, 31),
            dueDate: new DateOnly(2026, 8, 5),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid());

        var model = new ReceiptPdfModel(
            ReceiptNumber: "REC-2026-00892",
            IssueDate: DateTimeOffset.UtcNow,
            AmountPaid: 400.00m,
            Currency: "JOD",
            PaymentMethod: "Cash",
            ReferenceNumber: "REF-2026-0814",
            PaymentPurpose: "ScheduledInstallment",
            BillingPeriod: "01/08/2026 - 31/08/2026",
            DueDate: "05/08/2026",
            DueDateStatus: "Paid",
            TenantName: "أحمد العبداللات",
            TenantPhone: "+962 79 1234567",
            PropertyName: "برج الأمل السكني - عبدون",
            UnitNumber: "شقة 4B",
            ContractNumber: "CNT-2026-042");

        // Act
        var pdfBytes = await _generator.GenerateReceiptPdfAsync(rentPayment, model, CancellationToken.None);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "Generated PDF should be of substantial size.");

        // Check PDF magic header %PDF-
        var header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_WithChequeDetails_ProducesValidPdfBytes()
    {
        // Arrange
        var rentPayment = RentPayment.Create(
            companyId: Guid.NewGuid(),
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.ScheduledInstallment,
            amountDue: 1200.00m,
            currency: "JOD",
            billingPeriodStart: new DateOnly(2026, 9, 1),
            billingPeriodEnd: new DateOnly(2026, 11, 30),
            dueDate: new DateOnly(2026, 9, 1),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid());

        var model = new ReceiptPdfModel(
            ReceiptNumber: "REC-2026-00994",
            IssueDate: DateTimeOffset.UtcNow,
            AmountPaid: 1200.00m,
            Currency: "JOD",
            PaymentMethod: "Cheque",
            ReferenceNumber: "CHQ-REF-9921",
            PaymentPurpose: "ScheduledInstallment",
            BillingPeriod: "01/09/2026 - 30/11/2026",
            DueDate: "01/09/2026",
            DueDateStatus: "Paid",
            TenantName: "سليمان الشوابكة",
            TenantPhone: "+962 78 9876543",
            PropertyName: "مجمع الرابية التجاري",
            UnitNumber: "مكتب 102",
            ContractNumber: "CNT-2026-088",
            ChequeNumber: "00049281",
            BankName: "البنك العربي (Arab Bank)",
            ChequeIssueDate: "15/08/2026",
            ChequeDueDate: "01/09/2026");

        // Act
        var pdfBytes = await _generator.GenerateReceiptPdfAsync(rentPayment, model, CancellationToken.None);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);

        var header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task GenerateSettlementStatementPdfAsync_WithMultipleTransactions_ProducesValidPdfBytes()
    {
        // Arrange
        var transactions = new List<SettlementTransactionItem>
        {
            new(
                Index: 1,
                PaymentDate: DateTimeOffset.UtcNow.AddDays(-10),
                Amount: 150.00m,
                PaymentMethod: "CliQ",
                ReferenceNumber: "CLIQ-2026-0810",
                ReceiptNumber: "REC-000018"),
            new(
                Index: 2,
                PaymentDate: DateTimeOffset.UtcNow,
                Amount: 170.00m,
                PaymentMethod: "BankTransfer",
                ReferenceNumber: "BNK-2026-0818",
                ReceiptNumber: "REC-000019")
        };

        var statementModel = new SettlementStatementPdfModel(
            StatementNumber: "SETTLE-REC-000019",
            StatementDate: DateTimeOffset.UtcNow,
            TenantName: "أحمد العبداللات",
            TenantPhone: "+962 79 1234567",
            PropertyName: "برج الأمل السكني - عبدون",
            UnitNumber: "شقة 4B",
            ContractNumber: "CNT-2026-042",
            BillingPeriod: "01/08/2026 - 31/08/2026",
            DueDate: "05/08/2026",
            TotalAmountDue: 320.00m,
            TotalAmountPaid: 320.00m,
            RemainingBalance: 0.00m,
            Currency: "JOD",
            Status: "Paid",
            Transactions: transactions);

        // Act
        var pdfBytes = await _generator.GenerateSettlementStatementPdfAsync(statementModel, CancellationToken.None);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "Settlement statement PDF should be of substantial size.");

        var header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task GenerateSettlementStatementPdfAsync_WithSingleDirectTransaction_ProducesValidPdfBytes()
    {
        // Arrange
        var transactions = new List<SettlementTransactionItem>
        {
            new(
                Index: 1,
                PaymentDate: DateTimeOffset.UtcNow,
                Amount: 400.00m,
                PaymentMethod: "Cash",
                ReferenceNumber: "CSH-2026-01",
                ReceiptNumber: "REC-000020")
        };

        var statementModel = new SettlementStatementPdfModel(
            StatementNumber: "SETTLE-REC-000020",
            StatementDate: DateTimeOffset.UtcNow,
            TenantName: "سليمان الشوابكة",
            TenantPhone: null,
            PropertyName: "مجمع الرابية التجاري",
            UnitNumber: "مكتب 102",
            ContractNumber: "CNT-2026-088",
            BillingPeriod: "01/09/2026 - 30/09/2026",
            DueDate: "01/09/2026",
            TotalAmountDue: 400.00m,
            TotalAmountPaid: 400.00m,
            RemainingBalance: 0.00m,
            Currency: "JOD",
            Status: "Paid",
            Transactions: transactions);

        // Act
        var pdfBytes = await _generator.GenerateSettlementStatementPdfAsync(statementModel, CancellationToken.None);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);

        var header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }
}
