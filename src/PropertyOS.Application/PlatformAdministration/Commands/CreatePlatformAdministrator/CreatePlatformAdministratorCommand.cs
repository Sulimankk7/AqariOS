using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Application.PlatformAdministration.Commands.CreatePlatformAdministrator;

public sealed record CreatePlatformAdministratorCommand(
    string FullName,
    string Email,
    string Password) : ICommand<PlatformAdministratorDto>;
