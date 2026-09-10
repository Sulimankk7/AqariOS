using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetPlatformAdministrators;

public sealed record GetPlatformAdministratorsQuery
    : ITransactionalRequest<IReadOnlyList<PlatformAdministratorDto>>;
