namespace PropertyOS.Domain.Identity.Enums;

public enum LoginStatus
{
    Success,
    FailedPassword,
    FailedLocked,
    FailedMfa,
    FailedNotFound
}
