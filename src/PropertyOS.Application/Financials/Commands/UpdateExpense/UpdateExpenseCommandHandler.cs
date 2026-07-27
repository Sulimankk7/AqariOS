using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;

namespace PropertyOS.Application.Financials.Commands.UpdateExpense;

public class UpdateExpenseCommandHandler : IRequestHandler<UpdateExpenseCommand, Unit>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateExpenseCommandHandler(
        IExpenseRepository expenseRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _expenseRepository = expenseRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var expense = await _expenseRepository.GetByIdAsync(request.Id, cancellationToken);
        if (expense == null)
            throw new NotFoundException($"Expense with ID {request.Id} was not found.");

        if (request.BuildingId.HasValue && request.BuildingId.Value != Guid.Empty)
        {
            var buildingExists = await _expenseRepository.BuildingExistsAsync(request.BuildingId.Value, companyId, cancellationToken);
            if (!buildingExists)
                throw new NotFoundException($"Building with ID {request.BuildingId.Value} was not found.");
        }

        expense.UpdateDetails(
            buildingId: request.BuildingId,
            category: request.Category,
            amount: request.Amount,
            expenseDate: request.ExpenseDate,
            paymentMethod: request.PaymentMethod,
            description: request.Description,
            vendorName: request.VendorName,
            invoiceNumber: request.InvoiceNumber,
            notes: request.Notes,
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId
        );

        return Unit.Value;
    }
}
