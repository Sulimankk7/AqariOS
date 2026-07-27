using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.CreateTenant;

/// <summary>
/// Creates a tenant person record. The tenant account/invitation flow (Module 3 user link)
/// is intentionally deferred: UserId always stays null here.
/// </summary>
public record CreateTenantCommand(
    string Name,
    string NationalId,
    string Phone,
    string? Occupation = null,
    string? Employer = null
) : ICommand<Guid>;
