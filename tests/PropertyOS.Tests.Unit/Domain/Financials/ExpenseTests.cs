using System;
using System.Collections.Generic;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Financials;

public class ExpenseTests
{
    [Fact]
    public void Create_WithValidValues_Succeeds()
    {
        var companyId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expense = Expense.Create(
            companyId,
            buildingId,
            ExpenseCategory.Elevator,
            450.00m,
            "JOD",
            today,
            ExpensePaymentMethod.BankTransfer,
            "Elevator maintenance service",
            "Otis Jordan",
            "INV-101",
            "Monthly servicing",
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        Assert.Equal(companyId, expense.CompanyId);
        Assert.Equal(buildingId, expense.BuildingId);
        Assert.Equal(ExpenseCategory.Elevator, expense.Category);
        Assert.Equal(450.00m, expense.Amount);
        Assert.Equal("JOD", expense.Currency);
        Assert.Equal(today, expense.ExpenseDate);
        Assert.Equal(ExpensePaymentMethod.BankTransfer, expense.PaymentMethod);
        Assert.Equal("Otis Jordan", expense.VendorName);
        Assert.Equal("INV-101", expense.InvoiceNumber);
        Assert.Equal("Elevator maintenance service", expense.Description);
        Assert.Equal("Monthly servicing", expense.Notes);
    }

    [Fact]
    public void Create_CompanyWideOverhead_BuildingIdIsNull_Succeeds()
    {
        var companyId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expense = Expense.Create(
            companyId,
            null, // company-wide overhead
            ExpenseCategory.Administrative,
            120.00m,
            "JOD",
            today,
            ExpensePaymentMethod.Cash,
            "Office stationery purchase"
        );

        Assert.Null(expense.BuildingId);
        Assert.Equal(ExpenseCategory.Administrative, expense.Category);
        Assert.Equal(120.00m, expense.Amount);
    }

    [Fact]
    public void Create_NegativeOrZeroAmount_ThrowsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => Expense.Create(
            companyId,
            null,
            ExpenseCategory.Other,
            -5.00m, // negative
            "JOD",
            today,
            ExpensePaymentMethod.Other,
            "Invalid expense"
        ));

        Assert.Throws<ArgumentException>(() => Expense.Create(
            companyId,
            null,
            ExpenseCategory.Other,
            0m, // zero
            "JOD",
            today,
            ExpensePaymentMethod.Other,
            "Invalid expense"
        ));
    }

    [Fact]
    public void Create_FutureExpenseDate_ThrowsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        Assert.Throws<ArgumentException>(() => Expense.Create(
            companyId,
            null,
            ExpenseCategory.Emergency,
            100.00m,
            "JOD",
            tomorrow, // future date
            ExpensePaymentMethod.Cash,
            "Future repair"
        ));
    }

    [Fact]
    public void Create_MissingDescription_ThrowsArgumentException()
    {
        var companyId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => Expense.Create(
            companyId,
            null,
            ExpenseCategory.Cleaning,
            50.00m,
            "JOD",
            today,
            ExpensePaymentMethod.Cash,
            "" // empty description
        ));
    }

    [Fact]
    public void UpdateDetails_WithValidValues_Succeeds()
    {
        var expense = Expense.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExpenseCategory.WaterTank,
            150.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            ExpensePaymentMethod.Cash,
            "Water tank cleaning"
        );

        var newBuildingId = Guid.NewGuid();
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));

        expense.UpdateDetails(
            newBuildingId,
            ExpenseCategory.Generator,
            200.00m,
            newDate,
            ExpensePaymentMethod.Cheque,
            "Generator fuel top-up",
            "Manaseer",
            "INV-Fuel-99",
            "Updated notes",
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        Assert.Equal(newBuildingId, expense.BuildingId);
        Assert.Equal(ExpenseCategory.Generator, expense.Category);
        Assert.Equal(200.00m, expense.Amount);
        Assert.Equal(newDate, expense.ExpenseDate);
        Assert.Equal(ExpensePaymentMethod.Cheque, expense.PaymentMethod);
        Assert.Equal("Generator fuel top-up", expense.Description);
        Assert.Equal("Manaseer", expense.VendorName);
        Assert.Equal("INV-Fuel-99", expense.InvoiceNumber);
        Assert.Equal("Updated notes", expense.Notes);
    }

    [Fact]
    public void AttachReceipt_ValidValues_Succeeds()
    {
        var expense = Expense.Create(
            Guid.NewGuid(),
            null,
            ExpenseCategory.Elevator,
            400.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            ExpensePaymentMethod.BankTransfer,
            "Elevator parts invoice"
        );

        var fileId = Guid.NewGuid();
        var receipt = expense.AttachReceipt(
            fileId,
            "RCPT-0001",
            400.00m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            Guid.NewGuid(),
            "Labor and parts scan",
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        Assert.Single(expense.Receipts);
        Assert.Contains(receipt, expense.Receipts);
        Assert.Equal(fileId, receipt.FileId);
        Assert.Equal("RCPT-0001", receipt.ReceiptNumber);
        Assert.Equal(400.00m, receipt.Amount);
    }

    [Fact]
    public void AttachReceipt_DuplicateFileId_ThrowsInvalidOperationException()
    {
        var expense = Expense.Create(
            Guid.NewGuid(),
            null,
            ExpenseCategory.Elevator,
            400.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            ExpensePaymentMethod.BankTransfer,
            "Elevator parts invoice"
        );

        var fileId = Guid.NewGuid();
        expense.AttachReceipt(fileId, "RCPT-0001", 200.00m, DateOnly.FromDateTime(DateTime.UtcNow), null, null, DateTimeOffset.UtcNow, null);

        Assert.Throws<InvalidOperationException>(() => expense.AttachReceipt(
            fileId, // duplicate fileId
            "RCPT-0002",
            200.00m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            null,
            DateTimeOffset.UtcNow,
            null
        ));
    }

    [Fact]
    public void AttachReceipt_InvalidInvariants_ThrowsArgumentException()
    {
        var expense = Expense.Create(
            Guid.NewGuid(),
            null,
            ExpenseCategory.Elevator,
            400.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            ExpensePaymentMethod.BankTransfer,
            "Elevator parts invoice"
        );

        // Negative Amount
        Assert.Throws<ArgumentException>(() => expense.AttachReceipt(
            Guid.NewGuid(),
            "RCPT-0001",
            -10.00m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            null,
            DateTimeOffset.UtcNow,
            null
        ));

        // Future Date
        Assert.Throws<ArgumentException>(() => expense.AttachReceipt(
            Guid.NewGuid(),
            "RCPT-0001",
            10.00m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            null,
            null,
            DateTimeOffset.UtcNow,
            null
        ));
    }

    [Fact]
    public void RemoveReceipt_SoftDeletesChildReceipt()
    {
        var expense = Expense.Create(
            Guid.NewGuid(),
            null,
            ExpenseCategory.WaterTank,
            150.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            ExpensePaymentMethod.Cash,
            "Water tank cleaning"
        );

        var receipt = expense.AttachReceipt(Guid.NewGuid(), "RCPT-100", 150.00m, DateOnly.FromDateTime(DateTime.UtcNow), null, null, DateTimeOffset.UtcNow, null);
        
        // Simulating EF Core assigning an Id
        var receiptIdProperty = typeof(ExpenseReceipt).GetProperty("Id");
        var receiptId = Guid.NewGuid();
        receiptIdProperty!.SetValue(receipt, receiptId);

        expense.RemoveReceipt(receiptId, DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.NotNull(receipt.DeletedAt);
        Assert.NotNull(receipt.DeletedBy);
    }

    [Fact]
    public void SoftDelete_CascadesToReceipts()
    {
        var expense = Expense.Create(
            Guid.NewGuid(),
            null,
            ExpenseCategory.WaterTank,
            150.00m,
            "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            ExpensePaymentMethod.Cash,
            "Water tank cleaning"
        );

        var receipt = expense.AttachReceipt(Guid.NewGuid(), "RCPT-100", 150.00m, DateOnly.FromDateTime(DateTime.UtcNow), null, null, DateTimeOffset.UtcNow, null);

        var deletedAt = DateTimeOffset.UtcNow;
        var deletedBy = Guid.NewGuid();
        expense.SoftDelete(deletedAt, deletedBy);

        Assert.Equal(deletedAt, expense.DeletedAt);
        Assert.Equal(deletedBy, expense.DeletedBy);
        Assert.Equal(deletedAt, receipt.DeletedAt);
        Assert.Equal(deletedBy, receipt.DeletedBy);
    }
}
