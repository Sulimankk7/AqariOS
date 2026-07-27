using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Application.Financials.Commands.CreateEfawateercomTransaction;

public class CreateEfawateercomTransactionCommandHandler : IRequestHandler<CreateEfawateercomTransactionCommand, Unit>
{
    private readonly IEfawateercomTransactionRepository _transactionRepository;
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateEfawateercomTransactionCommandHandler(
        IEfawateercomTransactionRepository transactionRepository,
        IRentPaymentRepository rentPaymentRepository,
        ICurrentUserContext currentUserContext)
    {
        _transactionRepository = transactionRepository;
        _rentPaymentRepository = rentPaymentRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(CreateEfawateercomTransactionCommand request, CancellationToken cancellationToken)
    {
        var rentPayment = await _rentPaymentRepository.GetByIdAsync(request.RentPaymentId, cancellationToken);
        if (rentPayment == null)
            throw new NotFoundException($"RentPayment with ID {request.RentPaymentId} was not found.");

        var transaction = EfawateercomTransaction.Create(
            companyId: rentPayment.CompanyId,
            rentPaymentId: request.RentPaymentId,
            externalTransactionId: request.ExternalTransactionId,
            requestTime: DateTimeOffset.UtcNow,
            amount: request.Amount,
            currency: rentPayment.Currency,
            paymentReference: request.PaymentReference,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: _currentUserContext.UserId
        );

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        // Persistence is fully owned by TransactionBehavior.
        // SaveChangesAsync is NOT called here — consistent with all Modules 1–6 handlers.
        return Unit.Value;
    }
}
