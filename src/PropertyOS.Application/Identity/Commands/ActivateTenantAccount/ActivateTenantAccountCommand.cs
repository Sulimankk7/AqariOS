using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.DTOs.Identity;

namespace PropertyOS.Application.Identity.Commands.ActivateTenantAccount;

/// <summary>
/// Public command allowing an invited tenant to activate their account and set their initial password.
/// </summary>
public record ActivateTenantAccountCommand(
    string ActivationToken,
    string Password
) : ICommand<LoginResponseDto>;
