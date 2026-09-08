using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;
namespace PropertyOS.Application.PlatformAdministration.Queries.GetContactRequestById;
public sealed record GetContactRequestByIdQuery(Guid Id) : ITransactionalRequest<ContactRequestDetailDto>;
