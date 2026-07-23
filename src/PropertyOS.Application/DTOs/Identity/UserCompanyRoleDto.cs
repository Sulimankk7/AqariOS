using System;

namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Data transfer object representing a user's membership role within a specific company.
/// </summary>
public class UserCompanyRoleDto
{
    /// <summary>
    /// Membership record identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Company identifier.
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Assigned role identifier.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Assigned role code.
    /// </summary>
    public string RoleCode { get; set; } = null!;

    /// <summary>
    /// Assigned role display name.
    /// </summary>
    public string RoleName { get; set; } = null!;

    /// <summary>
    /// Membership status string (e.g. Active, InvitedPending, Suspended).
    /// </summary>
    public string Status { get; set; } = null!;
}
