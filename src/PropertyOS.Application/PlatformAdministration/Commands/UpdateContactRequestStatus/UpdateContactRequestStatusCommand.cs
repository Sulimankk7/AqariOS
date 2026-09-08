using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;
namespace PropertyOS.Application.PlatformAdministration.Commands.UpdateContactRequestStatus;
public sealed record UpdateContactRequestStatusCommand(Guid Id, string Status) : ITransactionalRequest<ContactRequestDetailDto>;
