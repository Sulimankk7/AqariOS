namespace PropertyOS.Domain.Identity.Entities;

/// <summary>
/// Associates a user with a global role whose <see cref="Role.CompanyId"/> is null.
/// Company-scoped role assignments continue to use <see cref="UserCompanyRole"/>.
/// </summary>
public sealed class UserSystemRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public User? GrantedByUser { get; set; }
}
