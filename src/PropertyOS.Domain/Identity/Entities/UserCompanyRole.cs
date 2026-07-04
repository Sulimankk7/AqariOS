using PropertyOS.Domain.Common;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Domain.Identity.Entities;

public class UserCompanyRole : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid RoleId { get; set; }
    public MembershipStatus Status { get; set; } = MembershipStatus.InvitedPending;
    public DateTimeOffset InvitedAt { get; set; }
    public DateTimeOffset? JoinedAt { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public User User { get; set; } = null!;
    public Company Company { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
