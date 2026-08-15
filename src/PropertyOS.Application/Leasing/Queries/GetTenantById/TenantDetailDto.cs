using System;
using System.Collections.Generic;

namespace PropertyOS.Application.Leasing.Queries.GetTenantById;

public class TenantDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }
    public Guid? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public List<TenantFamilyMemberDto> FamilyMembers { get; set; } = new();
    public List<TenantEmergencyContactDto> EmergencyContacts { get; set; } = new();
    public List<TenantVehicleDto> Vehicles { get; set; } = new();
}
