using MediatR;
using PropertyOS.Application.DTOs.Identity;

namespace PropertyOS.Application.Identity.Commands.PasswordReset;

public sealed record RequestPasswordResetCommand(
    PasswordResetDeliveryMethod DeliveryMethod,
    string Identifier,
    string? ClientIp,
    string? UserAgent) : IRequest<PasswordResetRequestResponseDto>;

public sealed record VerifyPasswordResetOtpCommand(
    string Phone,
    string Code,
    string? ClientIp) : IRequest<PasswordResetOtpVerifyResponseDto>;

public sealed record CompletePasswordResetCommand(
    string ResetCredential,
    string NewPassword,
    string? ClientIp,
    string? UserAgent) : IRequest<PasswordResetCompleteResponseDto>;
