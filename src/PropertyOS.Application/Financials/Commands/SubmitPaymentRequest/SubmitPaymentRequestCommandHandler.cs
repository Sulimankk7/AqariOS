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
using PropertyOS.Domain.Common;

namespace PropertyOS.Application.Financials.Commands.SubmitPaymentRequest;

public class SubmitPaymentRequestCommandHandler : IRequestHandler<SubmitPaymentRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserContext _currentUser;
    private readonly IBusinessClock _clock;
    private readonly IPublisher _publisher;

    public SubmitPaymentRequestCommandHandler(
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

    public async Task<Guid> Handle(SubmitPaymentRequestCommand request, CancellationToken cancellationToken)
    {
        var rentPayment = await _context.RentPayments
            .Include(rp => rp.Submissions)
            .FirstOrDefaultAsync(rp => rp.Id == request.RentPaymentId, cancellationToken);

        if (rentPayment == null)
            throw new NotFoundException($"RentPayment with ID {request.RentPaymentId} was not found.");

        var submittedBy = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        // Hard Tenant Authorization (P0-2 Requirement)
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == rentPayment.TenantId, cancellationToken);
            
        if (tenant == null)
            throw new UnauthorizedAccessException("Tenant record not found.");
            
        if (tenant.CompanyId != rentPayment.CompanyId)
            throw new UnauthorizedAccessException("Company mismatch.");
            
        if (tenant.UserId != submittedBy)
            throw new UnauthorizedAccessException("User is not authorized for this tenant.");

        var submittedAt = _clock.UtcNow;

        rentPayment.SubmitForVerification(
            request.PaymentMethod,
            request.ReferenceNumber,
            request.ProofFileId,
            submittedBy,
            submittedAt);

        await _context.SaveChangesAsync(cancellationToken);

        // Publish domain events manually since there's no automated dispatcher in EF Core yet
        foreach (var domainEvent in rentPayment.DomainEvents)
        {
            var notification = (INotification)Activator.CreateInstance(
                typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                domainEvent)!;
            
            await _publisher.Publish(notification, cancellationToken);
        }

        var submission = rentPayment.Submissions.OrderByDescending(s => s.CreatedAt).First();
        return submission.Id;
    }
}
