using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.CreateTenantEmergencyContact;

public record CreateTenantEmergencyContactCommand(
    Guid TenantId,
    string Name,
    string RelationshipType,
    string Phone
) : ICommand<Guid>;
