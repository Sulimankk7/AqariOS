using PropertyOS.Domain.Common;
using PropertyOS.Domain.Companies;

namespace PropertyOS.Domain.Identity.Entities;

public class Role : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public bool IsSystem { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public Company? Company { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserCompanyRole> UserCompanyRoles { get; set; } = new List<UserCompanyRole>();
    public ICollection<UserSystemRole> UserSystemRoles { get; set; } = new List<UserSystemRole>();
}
