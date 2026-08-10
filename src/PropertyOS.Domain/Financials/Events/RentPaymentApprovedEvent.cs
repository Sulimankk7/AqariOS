using System;

using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Financials.Events;

public record RentPaymentApprovedEvent(Guid RentPaymentId, Guid SubmissionId, Guid VerifiedBy) : IDomainEvent;
