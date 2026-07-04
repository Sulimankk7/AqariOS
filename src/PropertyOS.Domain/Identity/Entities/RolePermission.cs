namespace PropertyOS.Domain.Identity.Entities;

public class RolePermission
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
    public User? GrantedByUser { get; set; }
}
