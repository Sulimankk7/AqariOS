namespace PropertyOS.Domain.Audit.Enums;

public enum AuditAction
{
    Create,
    Update,
    Delete,
    SoftDelete,
    Restore,
    Login,
    Logout,
    PermissionChange,
    Export,
    StatusChange
}
