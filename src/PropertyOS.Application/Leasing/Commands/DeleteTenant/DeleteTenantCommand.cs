using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenant;

public record DeleteTenantCommand(Guid TenantId) : ICommand;
