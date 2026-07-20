using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;

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
            throw new KeyNotFoundException($"Expense with ID {request.ExpenseId} was not found.");

        // Single atomic round-trip: lock row, evaluate reset policy, increment counter, format number.
        var receiptNumber = await _sequenceRepository.ReserveAndFormatNextReceiptNumberAsync(
            expense.CompanyId, cancellationToken);

        var receipt = expense.AttachReceipt(
            fileId: request.FileId,
            receiptNumber: receiptNumber,
            amount: request.Amount,
            issuedAt: request.IssuedAt,
            uploadedBy: _currentUserContext.UserId,
            description: request.Description,
            now: DateTimeOffset.UtcNow,
            createdBy: _currentUserContext.UserId
        );

        // Persistence is owned by TransactionBehavior; SaveChangesAsync is not called here.
        return receipt.Id;
    }
}
