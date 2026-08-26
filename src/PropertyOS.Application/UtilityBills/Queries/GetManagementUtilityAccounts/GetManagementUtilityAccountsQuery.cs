using MediatR;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.UtilityBills.Queries.Common;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Queries.GetManagementUtilityAccounts;

public sealed record GetManagementUtilityAccountsQuery(
    string? Cursor = null,
    int PageSize = 50,
    UtilityType? UtilityType = null,
    UtilitySyncStatus? SyncStatus = null,
    bool? IsActive = null,
    Guid? LeaseContractId = null,
    bool IncludeUnlinked = false)
    : IRequest<KeysetPage<ManagementUtilityAccountDto>>;
