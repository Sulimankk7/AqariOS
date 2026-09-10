namespace PropertyOS.Api.PlatformAdministration.Requests;

public sealed record CreatePlatformAdministratorRequest(
    string FullName,
    string Email,
    string Password);
