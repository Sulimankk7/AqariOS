using System;

using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Financials.Events;

public record RentPaymentSubmittedEvent(Guid RentPaymentId, Guid SubmissionId) : IDomainEvent;
