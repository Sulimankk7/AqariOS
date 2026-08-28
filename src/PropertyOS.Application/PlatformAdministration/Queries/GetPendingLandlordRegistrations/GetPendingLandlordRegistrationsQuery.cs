using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetPendingLandlordRegistrations;

public sealed record GetPendingLandlordRegistrationsQuery(int Page = 1, int PageSize = 20)
    : ITransactionalRequest<LandlordRegistrationPageDto>;
