using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Application.Financials.Commands.AttachExpenseReceipt;

public class AttachExpenseReceiptCommandHandler : IRequestHandler<AttachExpenseReceiptCommand, Guid>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICompanyReceiptSequenceRepository _sequenceRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public AttachExpenseReceiptCommandHandler(
        IExpenseRepository expenseRepository,
        ICompanyReceiptSequenceRepository sequenceRepository,
        ICurrentUserContext currentUserContext)
    {
        _expenseRepository = expenseRepository;
        _sequenceRepository = sequenceRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(AttachExpenseReceiptCommand request, CancellationToken cancellationToken)
    {
        var expense = await _expenseRepository.GetByIdAsync(request.ExpenseId, cancellationToken);
        if (expense == null)
            throw new NotFoundException($"Expense with ID {request.ExpenseId} was not found.");

        // Single atomic round-trip: lock row, evaluate reset policy, increment counter, format number.
        var receiptNumber = await _sequenceRepository.ReserveAndFormatNextReceiptNumberAsync(
            expense.CompanyId, cancellationToken);

        ExpenseReceipt receipt;
        try
        {
            receipt = expense.AttachReceipt(
                fileId: request.FileId,
                receiptNumber: receiptNumber,
                amount: request.Amount,
                issuedAt: request.IssuedAt,
                uploadedBy: _currentUserContext.UserId,
                description: request.Description,
                now: DateTimeOffset.UtcNow,
                createdBy: _currentUserContext.UserId
            );
        }
        catch (InvalidOperationException ex)
        {
            // Domain rule: the same file cannot be attached twice to one expense
            throw new BusinessRuleException(ex.Message, "EXPENSE_RECEIPT_DUPLICATE_FILE");
        }

        // Explicit Add: the receipt carries a client-generated ID and its parent expense is
        // already tracked, so navigation discovery alone would mark it Modified, not Added.
        await _expenseRepository.AddReceiptAsync(receipt, cancellationToken);

        // Persistence is owned by TransactionBehavior; SaveChangesAsync is not called here.
        return receipt.Id;
    }
}
