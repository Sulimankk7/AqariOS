using System;

namespace PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;

public class ProvisionTenantAccountResponseDto
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string? ActivationToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool EmailSent { get; set; }
    public bool SmsSent { get; set; }
}
