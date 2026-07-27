using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Commands.CreateExpense;
using PropertyOS.Application.Financials.Commands.UpdateExpense;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class ExpenseCommandHandlerTests
{
    private class FakeExpenseRepository : IExpenseRepository
    {
        public List<Expense> Expenses { get; } = new();
        public HashSet<Guid> ExistentBuildings { get; } = new();

        public Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Expenses.FirstOrDefault(e => e.Id == id));
        }

        public Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
        {
            Expenses.Add(expense);
            return Task.CompletedTask;
        }

        public Task AddReceiptAsync(ExpenseReceipt receipt, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<Expense>> GetByBuildingIdAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Expenses.Where(e => e.BuildingId == buildingId && e.CompanyId == companyId).ToList());
        }

        public Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistentBuildings.Contains(buildingId));
        }

        public Task<ExpenseDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ExpenseDto>> GetExpensesAsync(ExpenseFilterOptions filter, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeCompanyReceiptSequenceRepository : ICompanyReceiptSequenceRepository
    {
        public long CurrentNumber { get; set; } = 0;

        public Task<CompanyReceiptSequence?> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(CompanyReceiptSequence sequence, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> ReserveAndFormatNextReceiptNumberAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CurrentNumber++;
            return Task.FromResult($"EXP-REC-{CurrentNumber:D4}");
        }

        public Task<long> ReserveNextReceiptNumberAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CurrentNumber++;
            return Task.FromResult(CurrentNumber);
        }
    }

    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; } = Guid.NewGuid();
        public bool IsPlatformAdmin => false;
    }

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; } = Guid.NewGuid();
    }

    [Fact]
    public async Task Handle_CreateExpenseWithReceipts_Succeeds_GeneratesReceiptNumbers()
    {
        // Arrange
        var expenseRepo = new FakeExpenseRepository();
        var seqRepo = new FakeCompanyReceiptSequenceRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var buildingId = Guid.NewGuid();
        expenseRepo.ExistentBuildings.Add(buildingId);

        var handler = new CreateExpenseCommandHandler(expenseRepo, seqRepo, tenantCtx, userCtx);

        var command = new CreateExpenseCommand(
            BuildingId: buildingId,
            Category: ExpenseCategory.Elevator,
            Amount: 1000.00m,
            ExpenseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            PaymentMethod: ExpensePaymentMethod.Cheque,
            Description: "Elevator spare parts replacement",
            VendorName: "Otis Jordan",
            InvoiceNumber: "INV-102",
            Notes: "Priority repair",
            Receipts: new List<CreateExpenseReceiptDto>
            {
                new(Guid.NewGuid(), 500.00m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "Part A receipt"),
                new(Guid.NewGuid(), 500.00m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "Part B receipt")
            }
        );

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        // We assert that the expense was added to the repository collection successfully
        Assert.Single(expenseRepo.Expenses);

        var createdExpense = expenseRepo.Expenses.First();
        Assert.Equal(1000.00m, createdExpense.Amount);
        Assert.Equal(buildingId, createdExpense.BuildingId);
        Assert.Equal(2, createdExpense.Receipts.Count);

        // Validate atomic sequence formatting numbers
        var receipts = createdExpense.Receipts.ToList();
        Assert.Equal("EXP-REC-0001", receipts[0].ReceiptNumber);
        Assert.Equal("EXP-REC-0002", receipts[1].ReceiptNumber);
        Assert.Equal(2, seqRepo.CurrentNumber);
    }

    [Fact]
    public async Task Handle_CreateExpense_WithNonExistentBuilding_ThrowsNotFoundException()
    {
        // Arrange
        var expenseRepo = new FakeExpenseRepository();
        var seqRepo = new FakeCompanyReceiptSequenceRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();

        var handler = new CreateExpenseCommandHandler(expenseRepo, seqRepo, tenantCtx, userCtx);

        var command = new CreateExpenseCommand(
            BuildingId: Guid.NewGuid(), // Not added to ExistentBuildings
            Category: ExpenseCategory.Cleaning,
            Amount: 50.00m,
            ExpenseDate: DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentMethod: ExpensePaymentMethod.Cash,
            Description: "Office mop cleaning"
        );

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UpdateExpenseDetails_AfterReceiptsAttached_Succeeds()
    {
        // Arrange
        var expenseRepo = new FakeExpenseRepository();
        var userCtx = new FakeCurrentUserContext();
        var tenantCtx = new FakeTenantContext();

        var handler = new UpdateExpenseCommandHandler(expenseRepo, tenantCtx, userCtx);

        var companyId = tenantCtx.CompanyId ?? Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        expenseRepo.ExistentBuildings.Add(buildingId);

        var expense = Expense.Create(
            companyId: companyId,
            buildingId: buildingId,
            category: ExpenseCategory.WaterTank,
            amount: 150.00m,
            currency: "JOD",
            expenseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            paymentMethod: ExpensePaymentMethod.Cash,
            description: "Water tank cleaning",
            vendorName: "Local Plumber",
            invoiceNumber: "INV-Water-1",
            notes: null,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId
        );

        // Attach receipt
        expense.AttachReceipt(
            fileId: Guid.NewGuid(),
            receiptNumber: "EXP-REC-1001",
            amount: 150.00m,
            issuedAt: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            uploadedBy: userCtx.UserId,
            description: "Plumber receipt scan",
            now: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId
        );

        await expenseRepo.AddAsync(expense);

        var command = new UpdateExpenseCommand(
            Id: expense.Id,
            BuildingId: buildingId,
            Category: ExpenseCategory.Security,
            Amount: 200.00m, // Change amount
            ExpenseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            PaymentMethod: ExpensePaymentMethod.BankTransfer,
            Description: "Water tank cleaning cost update",
            VendorName: "Local Plumber",
            InvoiceNumber: "INV-Water-1",
            Notes: "Updated notes"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(MediatR.Unit.Value, result);
        Assert.Equal(ExpenseCategory.Security, expense.Category);
        Assert.Equal(200.00m, expense.Amount);
        Assert.Equal("Water tank cleaning cost update", expense.Description);
    }
}
