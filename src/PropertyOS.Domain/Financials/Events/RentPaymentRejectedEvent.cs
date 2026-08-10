using System;

using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Financials.Events;

public record RentPaymentRejectedEvent(Guid RentPaymentId, Guid SubmissionId, string Reason, Guid RejectedBy) : IDomainEvent;
