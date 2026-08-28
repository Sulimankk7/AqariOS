using PropertyOS.Application.DTOs.Identity;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

public sealed class BuildPendingRegistrationResponseStep : ITenantProvisioningStep
{
    public int Order => 80;

    public Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        var registration = context.Registration
            ?? throw new InvalidOperationException("Registration must be created before producing the response.");

        context.Response = new RegisterResponseDto
        {
            RegistrationId = registration.Id,
            Status = registration.Status.ToString(),
            SubmittedAt = registration.SubmittedAt,
            Message = "Registration submitted and pending platform administrator approval."
        };

        return Task.CompletedTask;
    }
}
