using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Application.Financials.Commands.CreateExpense;

public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, Guid>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICompanyReceiptSequenceRepository _sequenceRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateExpenseCommandHandler(
        IExpenseRepository expenseRepository,
        ICompanyReceiptSequenceRepository sequenceRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _expenseRepository = expenseRepository;
        _sequenceRepository = sequenceRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        if (request.BuildingId.HasValue && request.BuildingId.Value != Guid.Empty)
        {
            var buildingExists = await _expenseRepository.BuildingExistsAsync(request.BuildingId.Value, companyId, cancellationToken);
            if (!buildingExists)
                throw new KeyNotFoundException($"Building with ID {request.BuildingId.Value} was not found.");
        }

        var expense = Expense.Create(
            companyId: companyId,
            buildingId: request.BuildingId,
            category: request.Category,
            amount: request.Amount,
            currency: "JOD",
            expenseDate: request.ExpenseDate,
            paymentMethod: request.PaymentMethod,
            description: request.Description,
            vendorName: request.VendorName,
            invoiceNumber: request.InvoiceNumber,
            notes: request.Notes,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: _currentUserContext.UserId
        );

        if (request.Receipts != null && request.Receipts.Count > 0)
        {
            // NOTE: At this point the expense has Id = Guid.Empty because the DB has not assigned
            // it yet. EF Core uses ValueGeneratedOnAdd with uuid_generate_v7() (server-side default).
            // When AddAsync is called, EF attaches the entity in the Added state; the DB assigns
            // the UUID on INSERT. The child ExpenseReceipt rows share the same DbContext tracking
            // graph and their expense_id FK is populated by EF's relationship fix-up on SaveChanges.
            // Therefore receipt attachment at this point is safe — EF will correctly populate
            // the FK when it materialises the INSERT for expense + owned receipts together.
            foreach (var receiptDto in request.Receipts)
            {
                // Single atomic round-trip per receipt: lock row, evaluate reset policy, increment, format.
                var receiptNumber = await _sequenceRepository.ReserveAndFormatNextReceiptNumberAsync(
                    companyId, cancellationToken);

                expense.AttachReceipt(
                    fileId: receiptDto.FileId,
                    receiptNumber: receiptNumber,
                    amount: receiptDto.Amount,
                    issuedAt: receiptDto.IssuedAt,
                    uploadedBy: _currentUserContext.UserId,
                    description: receiptDto.Description,
                    now: DateTimeOffset.UtcNow,
                    createdBy: _currentUserContext.UserId
                );
            }
        }

        // Adding the aggregate root to the context is sufficient; EF will cascade-insert owned receipts.
        await _expenseRepository.AddAsync(expense, cancellationToken);

        // Persistence is owned by TransactionBehavior; SaveChangesAsync is not called here.
        return expense.Id;
    }
}
