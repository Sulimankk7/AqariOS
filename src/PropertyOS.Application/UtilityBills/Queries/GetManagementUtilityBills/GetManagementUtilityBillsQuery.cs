using MediatR;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.UtilityBills.Queries.Common;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Queries.GetManagementUtilityBills;

public sealed record GetManagementUtilityBillsQuery(
    Guid UtilityAccountId,
    string? Cursor = null,
    int PageSize = 50,
    UtilityBillPaymentStatus? PaymentStatus = null)
    : IRequest<KeysetPage<UtilityBillDto>>;
