using System;
using System.Collections.Generic;

namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Data transfer object containing user identity, active company role context, and granted permission catalog.
/// </summary>
public class UserProfileDto
{
    /// <summary>
    /// User unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User email address, if present.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User primary phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// User full display name.
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// Preferred UI language (ar / en).
    /// </summary>
    public string PreferredLanguage { get; set; } = "ar";

    /// <summary>
    /// Indicates whether Multi-Factor Authentication is enabled.
    /// </summary>
    public bool MfaEnabled { get; set; }

    /// <summary>
    /// Active Company ID for the user context.
    /// </summary>
    public Guid? ActiveCompanyId { get; set; }

    /// <summary>
    /// List of company roles assigned to the user.
    /// </summary>
    public IReadOnlyList<UserCompanyRoleDto> CompanyRoles { get; set; } = new List<UserCompanyRoleDto>();

    /// <summary>
    /// List of granted permission keys (e.g., "documents.view_confidential", "properties.manage").
    /// </summary>
    public IReadOnlyList<string> Permissions { get; set; } = new List<string>();
}
