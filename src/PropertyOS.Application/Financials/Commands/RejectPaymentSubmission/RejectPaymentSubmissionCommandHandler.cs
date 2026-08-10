using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Application.Financials.Commands.RejectPaymentSubmission;

public class RejectPaymentSubmissionCommandHandler : IRequestHandler<RejectPaymentSubmissionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserContext _currentUser;
    private readonly IBusinessClock _clock;
    private readonly IPublisher _publisher;

    public RejectPaymentSubmissionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserContext currentUser,
        IBusinessClock clock,
        IPublisher publisher)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _publisher = publisher;
    }

    public async Task Handle(RejectPaymentSubmissionCommand request, CancellationToken cancellationToken)
    {
        var rentPayment = await _context.RentPayments
            .Include(rp => rp.Submissions)
            .FirstOrDefaultAsync(rp => rp.Submissions.Any(s => s.Id == request.SubmissionId), cancellationToken);

        if (rentPayment == null)
            throw new NotFoundException($"PaymentSubmission with ID {request.SubmissionId} was not found.");

        var rejectedBy = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var rejectedAt = _clock.UtcNow;

        rentPayment.RejectSubmission(request.SubmissionId, request.Reason, rejectedBy, rejectedAt);

        await _context.SaveChangesAsync(cancellationToken);

        // Publish domain events
        foreach (var domainEvent in rentPayment.DomainEvents)
        {
            var notification = (INotification)Activator.CreateInstance(
                typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                domainEvent)!;
            
            await _publisher.Publish(notification, cancellationToken);
        }
    }
}
