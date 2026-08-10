using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.UpdateTenantEmergencyContact;

public record UpdateTenantEmergencyContactCommand(
    Guid TenantId,
    Guid ContactId,
    string Name,
    string RelationshipType,
    string Phone
) : ICommand;
