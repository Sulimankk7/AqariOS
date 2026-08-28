using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetLandlordRegistrationById;

public sealed record GetLandlordRegistrationByIdQuery(Guid RegistrationId)
    : ITransactionalRequest<LandlordRegistrationDetailDto>;
