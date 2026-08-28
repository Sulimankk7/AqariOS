using System;
using System.Collections.Generic;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity.Commands.Register;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Application.Identity.Provisioning;

/// <summary>
/// Execution state passed through the tenant provisioning pipeline steps.
/// Shares created entities, authentication context, and intermediate state.
/// </summary>
public class TenantProvisioningContext
{
    public RegisterCommand Command { get; }
    public DateTimeOffset CreatedAt { get; }

    public User? User { get; set; }
    public Company? Company { get; set; }
    public CompanySettings? CompanySettings { get; set; }
    public Role? AdminRole { get; set; }
    public UserCompanyRole? UserCompanyRole { get; set; }
    public LandlordRegistration? Registration { get; set; }
    public RegisterResponseDto? Response { get; set; }

    /// <summary>
    /// Property bag for extending step state in future provisioning modules (e.g. Subscriptions, Storage Quotas).
    /// </summary>
    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();

    public TenantProvisioningContext(RegisterCommand command, DateTimeOffset createdAt)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        CreatedAt = createdAt;
    }
}
