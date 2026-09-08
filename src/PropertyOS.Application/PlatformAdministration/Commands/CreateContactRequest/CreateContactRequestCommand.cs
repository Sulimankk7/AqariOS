using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Application.PlatformAdministration.Commands.CreateContactRequest;

public sealed record CreateContactRequestCommand(string Name, string CompanyName, string PhoneNumber, int NumberOfBuildings, string? Notes) : ITransactionalRequest<ContactRequestCreatedDto>;
