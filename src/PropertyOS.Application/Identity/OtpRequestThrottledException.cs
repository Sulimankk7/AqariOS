namespace PropertyOS.Application.Identity;

public sealed class OtpRequestThrottledException : Exception
{
    public OtpRequestThrottledException(int retryAfterSeconds)
        : base("Please wait before requesting another verification code.")
    {
        RetryAfterSeconds = Math.Max(1, retryAfterSeconds);
    }

    public int RetryAfterSeconds { get; }
}
