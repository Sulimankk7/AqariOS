namespace PropertyOS.Application.Identity;

public sealed class OtpDeliveryException : Exception
{
    public OtpDeliveryException() : base("The verification code could not be delivered. Please try again.") { }
}
