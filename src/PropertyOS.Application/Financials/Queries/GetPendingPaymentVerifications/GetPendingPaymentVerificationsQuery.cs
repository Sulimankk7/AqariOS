using MediatR;
using PropertyOS.Application.Common.Models;

namespace PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications;

public record GetPendingPaymentVerificationsQuery(
    string? Cursor = null,
    int PageSize = 50
) : IRequest<KeysetPage<PaymentVerificationQueueItemDto>>;
