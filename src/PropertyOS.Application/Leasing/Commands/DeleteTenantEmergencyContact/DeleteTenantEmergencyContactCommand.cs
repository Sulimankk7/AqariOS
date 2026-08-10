using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenantEmergencyContact;

public record DeleteTenantEmergencyContactCommand(
    Guid TenantId,
    Guid ContactId
) : ICommand;
