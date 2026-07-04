namespace PropertyOS.Domain.Identity.Entities;

public class Permission
{
    public Guid Id { get; set; }
    public string Key { get; set; } = null!;
    public string Module { get; set; } = null!;
    public string DescriptionEn { get; set; } = null!;
    public string DescriptionAr { get; set; } = null!;
    public bool IsDeprecated { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
