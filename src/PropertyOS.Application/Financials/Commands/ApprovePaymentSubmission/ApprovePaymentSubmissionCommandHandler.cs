using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.Financials.Commands.RecordManualRentPayment;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Application.Financials.Commands.ApprovePaymentSubmission;

public class ApprovePaymentSubmissionCommandHandler : IRequestHandler<ApprovePaymentSubmissionCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserContext _currentUser;
    private readonly IBusinessClock _clock;
    private readonly IPublisher _publisher;
    private readonly ISender _sender;

    public ApprovePaymentSubmissionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserContext currentUser,
        IBusinessClock clock,
        IPublisher publisher,
        ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _publisher = publisher;
        _sender = sender;
    }

    public async Task<Unit> Handle(ApprovePaymentSubmissionCommand request, CancellationToken cancellationToken)
    {
        var rentPayment = await _context.RentPayments
            .Include(rp => rp.Submissions)
            .FirstOrDefaultAsync(rp => rp.Submissions.Any(s => s.Id == request.SubmissionId), cancellationToken);

        if (rentPayment == null)
            throw new NotFoundException($"PaymentSubmission with ID {request.SubmissionId} was not found.");

        var submission = rentPayment.Submissions.FirstOrDefault(s => s.Id == request.SubmissionId)
            ?? throw new NotFoundException($"PaymentSubmission with ID {request.SubmissionId} was not found.");

        if (submission.Status != PropertyOS.Domain.Financials.Enums.SubmissionStatus.Pending)
            throw new BusinessRuleException("Only pending submissions can be approved.", "SUBMISSION_NOT_PENDING");

        var currentOutstandingBalance = rentPayment.AmountDue - rentPayment.AmountPaid;
        if (currentOutstandingBalance <= 0 || rentPayment.DueDateStatus == PropertyOS.Domain.Financials.Enums.DueDateStatus.Paid)
        {
            throw new BusinessRuleException("This rent installment is already fully settled.", "INSTALLMENT_ALREADY_PAID");
        }

        // Exact submitted amount integrity (FIN-01): allocate submission.Amount (fallback to remaining balance only for legacy rows where Amount is null)
        var amountToAllocate = submission.Amount ?? currentOutstandingBalance;

        if (amountToAllocate <= 0)
        {
            throw new BusinessRuleException("Submitted payment amount must be greater than zero.", "INVALID_SUBMISSION_AMOUNT");
        }

        if (amountToAllocate > currentOutstandingBalance)
        {
            throw new BusinessRuleException(
                $"Submitted payment amount ({amountToAllocate} {rentPayment.Currency}) exceeds the current outstanding balance ({currentOutstandingBalance} {rentPayment.Currency}).",
                "AMOUNT_EXCEEDS_OUTSTANDING_BALANCE");
        }

        var verifiedBy = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var verifiedAt = _clock.UtcNow;

        // Invoke the canonical Module 6 financial engine to handle the actual settlement.
        // Allocation targets MUST be ScheduledInstallments per core domain rule.
        Guid? receivingPaymentId = null;
        if (amountToAllocate > 0)
        {
            System.Collections.Generic.List<AllocationDetail>? allocations = null;
            if (rentPayment.PaymentPurpose == PropertyOS.Domain.Financials.Enums.PaymentPurpose.ScheduledInstallment)
            {
                allocations = new System.Collections.Generic.List<AllocationDetail>
                {
                    new AllocationDetail(rentPayment.Id, amountToAllocate)
                };
            }

            ManualChequeDetails? chequeDetails = null;
            if (submission.PaymentMethod == PropertyOS.Domain.Financials.Enums.PaymentMethod.Cheque)
            {
                if (string.IsNullOrWhiteSpace(submission.ChequeNumber) ||
                    string.IsNullOrWhiteSpace(submission.BankName) ||
                    !submission.ChequeIssueDate.HasValue ||
                    !submission.ChequeDueDate.HasValue)
                {
                    throw new BusinessRuleException(
                        "Cheque details are required before this payment can be approved.",
                        "CHEQUE_DETAILS_REQUIRED");
                }

                var issueDate = submission.ChequeIssueDate.Value;
                var dueDate = submission.ChequeDueDate.Value;

                chequeDetails = new ManualChequeDetails(
                    ChequeNumber: submission.ChequeNumber,
                    BankName: submission.BankName,
                    BankBranch: null,
                    IssueDate: issueDate,
                    DueDate: dueDate >= issueDate ? dueDate : issueDate,
                    ReceivedDate: DateOnly.FromDateTime(submission.SubmittedAt.DateTime)
                );
            }

            var recordPaymentCommand = new RecordManualRentPaymentCommand(
                LeaseContractId: rentPayment.LeaseContractId,
                Amount: amountToAllocate,
                PaymentMethod: submission.PaymentMethod,
                PaymentReferenceNumber: submission.ReferenceNumber,
                Notes: "Payment submitted via Tenant Portal and approved by owner",
                Cheque: chequeDetails,
                Allocations: allocations
            );

            receivingPaymentId = await _sender.Send(recordPaymentCommand, cancellationToken);
        }

        rentPayment.ApproveSubmission(request.SubmissionId, verifiedBy, verifiedAt, receivingPaymentId);

        // Publish domain events
        foreach (var domainEvent in rentPayment.DomainEvents)
        {
            var notification = (INotification)Activator.CreateInstance(
                typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                domainEvent)!;
            
            await _publisher.Publish(notification, cancellationToken);
        }

        return Unit.Value;
    }
}
