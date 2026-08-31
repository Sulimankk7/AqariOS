using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Options;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Domain.Audit.Entities;
using PropertyOS.Domain.Audit.Enums;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Application.Identity.Commands.PasswordReset;

public sealed class RequestPasswordResetCommandHandler
    : IRequestHandler<RequestPasswordResetCommand, PasswordResetRequestResponseDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly PasswordResetOptions _options;
    private readonly OtpOptions _otpOptions;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

    public RequestPasswordResetCommandHandler(
        IApplicationDbContext dbContext,
        IEmailSender emailSender,
        ISmsSender smsSender,
        IOptions<PasswordResetOptions> options,
        IOptions<OtpOptions> otpOptions,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<RequestPasswordResetCommandHandler> logger)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _options = options.Value;
        _otpOptions = otpOptions.Value;
        _frontendOptions = frontendOptions.Value;
        _logger = logger;
    }

    public async Task<PasswordResetRequestResponseDto> Handle(
        RequestPasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
        var response = new PasswordResetRequestResponseDto();
        var now = DateTimeOffset.UtcNow;

        async Task<PasswordResetRequestResponseDto> FinishAsync()
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startedAt);
            var minimum = TimeSpan.FromMilliseconds(Math.Max(0, _options.MinimumRequestDurationMilliseconds));
            if (elapsed < minimum)
                await Task.Delay(minimum - elapsed, cancellationToken);
            return response;
        }

        string normalizedIdentifier;
        PasswordResetChallengeKind kind;
        User? user;

        if (request.DeliveryMethod == PasswordResetDeliveryMethod.Email)
        {
            normalizedIdentifier = NormalizeEmail(request.Identifier);
            var maskedEmail = MaskEmail(normalizedIdentifier);
            _logger.LogInformation(
                "Password reset email request received. Stage=RequestReceived Recipient={RecipientMasked}",
                maskedEmail);
            kind = PasswordResetChallengeKind.EmailToken;
            user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(
                candidate => candidate.Email == normalizedIdentifier && candidate.IsActive && candidate.DeletedAt == null,
                cancellationToken);

            if (user != null)
            {
                _logger.LogInformation(
                    "Password reset email eligible user found. Stage=EligibleUserFound Recipient={RecipientMasked} UserId={UserId}",
                    maskedEmail,
                    user.Id);
            }
        }
        else
        {
            if (!OtpPhoneNumber.TryNormalize(request.Identifier, out normalizedIdentifier, allowJordanianLocal: true))
                return await FinishAsync();
            kind = PasswordResetChallengeKind.SmsOtp;
            user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(
                candidate => candidate.Phone == normalizedIdentifier && candidate.IsActive && candidate.DeletedAt == null,
                cancellationToken);
        }

        if (user == null)
        {
            // Perform the same credential-hash primitives used by real requests without
            // persisting or delivering anything. The public response remains identical.
            _ = kind == PasswordResetChallengeKind.EmailToken
                ? PasswordResetSecurity.HashToken(PasswordResetSecurity.GenerateToken())
                : PasswordResetSecurity.HashOtp("000000", _otpOptions.HashKey);
            _logger.LogInformation(
                "Password reset request completed without delivery. Stage=EligibleUserNotFound Method={Method} Destination={DestinationMasked}",
                request.DeliveryMethod,
                MaskIdentifier(normalizedIdentifier, request.DeliveryMethod));
            return await FinishAsync();
        }

        var latest = await _dbContext.PasswordResetChallenges.AsNoTracking()
            .Where(challenge => challenge.UserId == user.Id && challenge.Kind == kind)
            .OrderByDescending(challenge => challenge.CreatedAt)
            .Select(challenge => new { challenge.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (latest != null && (now - latest.CreatedAt).TotalSeconds < _options.ResendCooldownSeconds)
        {
            _logger.LogInformation(
                "Password reset request completed without delivery. Stage=ResendCooldown Method={Method} Destination={DestinationMasked} UserId={UserId}",
                request.DeliveryMethod,
                MaskIdentifier(normalizedIdentifier, request.DeliveryMethod),
                user.Id);
            return await FinishAsync();
        }

        var windowStart = now.AddMinutes(-_options.DestinationWindowMinutes);
        var requestCount = await _dbContext.PasswordResetChallenges.CountAsync(
            challenge => challenge.UserId == user.Id && challenge.Kind == kind && challenge.CreatedAt >= windowStart,
            cancellationToken);
        if (requestCount >= _options.MaxRequestsPerDestinationWindow)
        {
            _logger.LogInformation(
                "Password reset request completed without delivery. Stage=DestinationRequestLimit Method={Method} Destination={DestinationMasked} UserId={UserId}",
                request.DeliveryMethod,
                MaskIdentifier(normalizedIdentifier, request.DeliveryMethod),
                user.Id);
            return await FinishAsync();
        }

        string rawCredential;
        string credentialHash;
        DateTimeOffset expiresAt;

        if (kind == PasswordResetChallengeKind.EmailToken)
        {
            rawCredential = PasswordResetSecurity.GenerateToken();
            credentialHash = PasswordResetSecurity.HashToken(rawCredential);
            expiresAt = now.AddMinutes(_options.EmailTokenExpiryMinutes);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_otpOptions.HashKey))
                throw new InvalidOperationException("OTP security configuration is unavailable.");
            rawCredential = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            credentialHash = PasswordResetSecurity.HashOtp(rawCredential, _otpOptions.HashKey);
            expiresAt = now.AddMinutes(_options.SmsOtpExpiryMinutes);
        }

        var challenge = new PasswordResetChallenge
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Kind = kind,
            CredentialHash = credentialHash,
            ExpiresAt = expiresAt,
            FailedAttempts = 0,
            MaxAttempts = kind == PasswordResetChallengeKind.SmsOtp ? _options.MaxOtpAttempts : (short)1,
            RequestedIp = PasswordResetSecurity.ParseIp(request.ClientIp),
            CreatedAt = now
        };
        async Task PersistChallengeAsync(CancellationToken ct)
        {
            await ConsumeActiveChallengesAsync(user.Id, kind, now, ct);
            _dbContext.PasswordResetChallenges.Add(challenge);
            AddAudit(user.Id, challenge.Id, "requested", request.DeliveryMethod.ToString(), request.ClientIp, request.UserAgent, now);
        }

        if (_dbContext.SupportsAtomicOperations)
            await _dbContext.ExecuteInTransactionAsync(PersistChallengeAsync, cancellationToken);
        else
        {
            await PersistChallengeAsync(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Password reset challenge created. Stage=ChallengeCreated Method={Method} Destination={DestinationMasked} UserId={UserId} ChallengeId={ChallengeId}",
            request.DeliveryMethod,
            MaskIdentifier(normalizedIdentifier, request.DeliveryMethod),
            user.Id,
            challenge.Id);

        bool sent;
        if (kind == PasswordResetChallengeKind.EmailToken)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_frontendOptions.BaseUrl)
                ? "http://localhost:5173"
                : _frontendOptions.BaseUrl.TrimEnd('/');
            var resetUrl = $"{baseUrl}/auth/reset-password?token={Uri.EscapeDataString(rawCredential)}";
            _logger.LogInformation(
                "Invoking transactional email sender for password reset. Stage=EmailSenderInvoked Recipient={RecipientMasked} UserId={UserId} ChallengeId={ChallengeId}",
                MaskEmail(normalizedIdentifier),
                user.Id,
                challenge.Id);
            sent = await _emailSender.SendAsync(
                normalizedIdentifier,
                "إعادة تعيين كلمة المرور في عقاري",
                BuildEmailHtml(resetUrl),
                $"استخدم الرابط التالي لإعادة تعيين كلمة المرور. تنتهي صلاحيته خلال {_options.EmailTokenExpiryMinutes} دقيقة:\n{resetUrl}",
                cancellationToken);
        }
        else
        {
            var message = $"رمز إعادة تعيين كلمة المرور في AqariOS هو: {rawCredential}\nالرمز صالح لمدة {_options.SmsOtpExpiryMinutes} دقائق.";
            sent = await _smsSender.SendAsync(normalizedIdentifier, message, cancellationToken);
        }

        if (!sent)
        {
            var failedAt = DateTimeOffset.UtcNow;
            challenge.ConsumedAt = failedAt;
            AddAudit(user.Id, challenge.Id, "delivery_failed", request.DeliveryMethod.ToString(), request.ClientIp, request.UserAgent, failedAt);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogWarning(
                "Password reset delivery was not accepted and its challenge was consumed. Stage=DeliveryFailedChallengeConsumed Method={Method} Destination={DestinationMasked} UserId={UserId} ChallengeId={ChallengeId}",
                request.DeliveryMethod,
                MaskIdentifier(normalizedIdentifier, request.DeliveryMethod),
                user.Id,
                challenge.Id);
        }

        return await FinishAsync();
    }

    private async Task ConsumeActiveChallengesAsync(
        Guid userId,
        PasswordResetChallengeKind kind,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken)
    {
        var active = _dbContext.PasswordResetChallenges
            .Where(challenge => challenge.UserId == userId && challenge.Kind == kind && challenge.ConsumedAt == null);
        if (_dbContext.SupportsAtomicOperations)
        {
            await active.ExecuteUpdateAsync(
                setters => setters.SetProperty(challenge => challenge.ConsumedAt, consumedAt),
                cancellationToken);
        }
        else
        {
            foreach (var challenge in await active.ToListAsync(cancellationToken))
                challenge.ConsumedAt = consumedAt;
        }
    }

    private void AddAudit(
        Guid userId,
        Guid challengeId,
        string eventName,
        string method,
        string? clientIp,
        string? userAgent,
        DateTimeOffset occurredAt)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.CreateVersion7(),
            ActorUserId = userId,
            EntityName = "PasswordReset",
            EntityId = challengeId,
            Action = AuditAction.Create,
            OccurredAt = occurredAt,
            CreatedAt = occurredAt,
            IpAddress = PasswordResetSecurity.ParseIp(clientIp),
            UserAgent = userAgent,
            Severity = AuditSeverity.Info,
            Source = AuditSource.Api,
            Metadata = JsonSerializer.Serialize(new { Event = eventName, DeliveryMethod = method })
        });
    }

    private static string NormalizeEmail(string value)
    {
        try
        {
            return new MailAddress(value.Trim()).Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            return value.Trim().ToLowerInvariant();
        }
    }

    private static string MaskIdentifier(string identifier, PasswordResetDeliveryMethod method) =>
        method == PasswordResetDeliveryMethod.Email ? MaskEmail(identifier) : MaskPhone(identifier);

    private static string MaskEmail(string email)
    {
        var separatorIndex = email.IndexOf('@');
        if (separatorIndex <= 0 || separatorIndex == email.Length - 1)
            return "[INVALID]";

        var local = email[..separatorIndex];
        var domain = email[(separatorIndex + 1)..];
        var maskedLocal = local.Length == 1 ? "*" : $"{local[0]}***{local[^1]}";
        var maskedDomain = domain.Length <= 2 ? "**" : $"{domain[0]}***{domain[^1]}";
        return $"{maskedLocal}@{maskedDomain}";
    }

    private static string MaskPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "[INVALID]";

        var visibleSuffixLength = Math.Min(3, phone.Length);
        return $"{phone[..Math.Min(4, phone.Length)]}***{phone[^visibleSuffixLength..]}";
    }

    private static string BuildEmailHtml(string resetUrl)
    {
        var safeUrl = System.Net.WebUtility.HtmlEncode(resetUrl);
        return $"<div dir=\"rtl\"><h2>إعادة تعيين كلمة المرور</h2><p>اضغط على الرابط التالي لتعيين كلمة مرور جديدة.</p><p><a href=\"{safeUrl}\">إعادة تعيين كلمة المرور</a></p><p>إذا لم تطلب ذلك، تجاهل الرسالة.</p></div>";
    }
}

public sealed class VerifyPasswordResetOtpCommandHandler
    : IRequestHandler<VerifyPasswordResetOtpCommand, PasswordResetOtpVerifyResponseDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly PasswordResetOptions _options;
    private readonly OtpOptions _otpOptions;

    public VerifyPasswordResetOtpCommandHandler(
        IApplicationDbContext dbContext,
        IOptions<PasswordResetOptions> options,
        IOptions<OtpOptions> otpOptions)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _otpOptions = otpOptions.Value;
    }

    public async Task<PasswordResetOtpVerifyResponseDto> Handle(
        VerifyPasswordResetOtpCommand request,
        CancellationToken cancellationToken)
    {
        if (!OtpPhoneNumber.TryNormalize(request.Phone, out var phone, allowJordanianLocal: true))
            throw ResetError("Password reset code is invalid.", "PASSWORD_RESET_OTP_INVALID");

        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(
            candidate => candidate.Phone == phone && candidate.IsActive && candidate.DeletedAt == null,
            cancellationToken);
        if (user == null)
            throw ResetError("Password reset code is invalid.", "PASSWORD_RESET_OTP_INVALID");

        var challenge = await _dbContext.PasswordResetChallenges
            .Where(candidate => candidate.UserId == user.Id && candidate.Kind == PasswordResetChallengeKind.SmsOtp)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge == null || challenge.ConsumedAt != null)
            throw ResetError("Password reset code is invalid or already used.", "PASSWORD_RESET_OTP_INVALID");
        if (challenge.ExpiresAt <= DateTimeOffset.UtcNow)
            throw ResetError("Password reset code has expired.", "PASSWORD_RESET_OTP_EXPIRED");
        if (challenge.FailedAttempts >= challenge.MaxAttempts)
            throw ResetError("Password reset attempts exceeded.", "PASSWORD_RESET_OTP_ATTEMPTS_EXCEEDED");

        var suppliedHash = PasswordResetSecurity.HashOtp(request.Code.Trim(), _otpOptions.HashKey);
        if (!PasswordResetSecurity.FixedTimeEquals(challenge.CredentialHash, suppliedHash))
        {
            if (_dbContext.SupportsAtomicOperations)
            {
                await _dbContext.PasswordResetChallenges
                    .Where(candidate => candidate.Id == challenge.Id && candidate.FailedAttempts < candidate.MaxAttempts)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(candidate => candidate.FailedAttempts, candidate => (short)(candidate.FailedAttempts + 1)),
                        cancellationToken);
            }
            else
            {
                challenge.FailedAttempts++;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            throw ResetError("Password reset code is invalid.", "PASSWORD_RESET_OTP_INVALID");
        }

        var now = DateTimeOffset.UtcNow;
        var rawAuthorization = PasswordResetSecurity.GenerateToken();
        async Task AuthorizeAsync(CancellationToken ct)
        {
            var consumed = await ConsumeOtpAsync(challenge, now, request.ClientIp, ct);
            if (!consumed)
                throw ResetError("Password reset code is invalid or already used.", "PASSWORD_RESET_OTP_INVALID");

            await ConsumeActiveAuthorizationsAsync(user.Id, now, ct);
            _dbContext.PasswordResetChallenges.Add(new PasswordResetChallenge
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                Kind = PasswordResetChallengeKind.SmsAuthorization,
                CredentialHash = PasswordResetSecurity.HashToken(rawAuthorization),
                ExpiresAt = now.AddMinutes(_options.SmsAuthorizationExpiryMinutes),
                FailedAttempts = 0,
                MaxAttempts = 1,
                RequestedIp = PasswordResetSecurity.ParseIp(request.ClientIp),
                VerifiedIp = PasswordResetSecurity.ParseIp(request.ClientIp),
                CreatedAt = now
            });
        }

        if (_dbContext.SupportsAtomicOperations)
            await _dbContext.ExecuteInTransactionAsync(AuthorizeAsync, cancellationToken);
        else
        {
            await AuthorizeAsync(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new PasswordResetOtpVerifyResponseDto
        {
            ResetAuthorization = rawAuthorization,
            ExpiresInSeconds = _options.SmsAuthorizationExpiryMinutes * 60
        };
    }

    private async Task<bool> ConsumeOtpAsync(
        PasswordResetChallenge challenge,
        DateTimeOffset now,
        string? clientIp,
        CancellationToken cancellationToken)
    {
        if (_dbContext.SupportsAtomicOperations)
        {
            var count = await _dbContext.PasswordResetChallenges
                .Where(candidate => candidate.Id == challenge.Id && candidate.ConsumedAt == null &&
                                    candidate.ExpiresAt > now && candidate.FailedAttempts < candidate.MaxAttempts)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.ConsumedAt, now)
                    .SetProperty(candidate => candidate.VerifiedIp, PasswordResetSecurity.ParseIp(clientIp)), cancellationToken);
            return count == 1;
        }

        if (challenge.ConsumedAt != null || challenge.ExpiresAt <= now || challenge.FailedAttempts >= challenge.MaxAttempts)
            return false;
        challenge.ConsumedAt = now;
        challenge.VerifiedIp = PasswordResetSecurity.ParseIp(clientIp);
        return true;
    }

    private async Task ConsumeActiveAuthorizationsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = _dbContext.PasswordResetChallenges.Where(candidate =>
            candidate.UserId == userId && candidate.Kind == PasswordResetChallengeKind.SmsAuthorization && candidate.ConsumedAt == null);
        if (_dbContext.SupportsAtomicOperations)
            await active.ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.ConsumedAt, now), cancellationToken);
        else
            foreach (var authorization in await active.ToListAsync(cancellationToken)) authorization.ConsumedAt = now;
    }

    private static BusinessRuleException ResetError(string message, string code) => new(message, code);
}

public sealed class CompletePasswordResetCommandHandler
    : IRequestHandler<CompletePasswordResetCommand, PasswordResetCompleteResponseDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public CompletePasswordResetCommandHandler(IApplicationDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<PasswordResetCompleteResponseDto> Handle(
        CompletePasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        var hash = PasswordResetSecurity.HashToken(request.ResetCredential.Trim());
        var challenge = await _dbContext.PasswordResetChallenges.FirstOrDefaultAsync(
            candidate => candidate.CredentialHash == hash &&
                         (candidate.Kind == PasswordResetChallengeKind.EmailToken ||
                          candidate.Kind == PasswordResetChallengeKind.SmsAuthorization),
            cancellationToken);

        if (challenge == null)
            throw ResetError("Password reset credential is invalid.", "PASSWORD_RESET_TOKEN_INVALID");
        if (challenge.ConsumedAt != null)
            throw ResetError("Password reset credential has already been used.", "PASSWORD_RESET_TOKEN_USED");
        if (challenge.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            var code = challenge.Kind == PasswordResetChallengeKind.SmsAuthorization
                ? "PASSWORD_RESET_AUTHORIZATION_EXPIRED"
                : "PASSWORD_RESET_TOKEN_EXPIRED";
            throw ResetError("Password reset credential has expired.", code);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == challenge.UserId && candidate.IsActive && candidate.DeletedAt == null,
            cancellationToken);
        if (user == null)
            throw ResetError("Password reset credential is invalid.", "PASSWORD_RESET_TOKEN_INVALID");

        async Task CompleteAsync(CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            if (_dbContext.SupportsAtomicOperations)
            {
                var consumed = await _dbContext.PasswordResetChallenges
                    .Where(candidate => candidate.Id == challenge.Id && candidate.ConsumedAt == null && candidate.ExpiresAt > now)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.ConsumedAt, now), ct);
                if (consumed != 1)
                    throw ResetError("Password reset credential has already been used.", "PASSWORD_RESET_TOKEN_USED");
            }
            else
            {
                if (challenge.ConsumedAt != null || challenge.ExpiresAt <= now)
                    throw ResetError("Password reset credential has already been used.", "PASSWORD_RESET_TOKEN_USED");
                challenge.ConsumedAt = now;
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.PasswordAlgorithm = "argon2id";
            user.UpdatedAt = now;

            var activeRefreshTokens = await _dbContext.RefreshTokens
                .Where(token => token.UserId == user.Id && token.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var refreshToken in activeRefreshTokens)
            {
                refreshToken.RevokedAt = now;
                refreshToken.RevokedReason = RevokeReason.PasswordReset;
            }

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.CreateVersion7(),
                ActorUserId = user.Id,
                EntityName = "PasswordReset",
                EntityId = challenge.Id,
                Action = AuditAction.Update,
                OccurredAt = now,
                CreatedAt = now,
                IpAddress = PasswordResetSecurity.ParseIp(request.ClientIp),
                UserAgent = request.UserAgent,
                Severity = AuditSeverity.Warning,
                Source = AuditSource.Api,
                Metadata = JsonSerializer.Serialize(new { Event = "completed", ChallengeKind = challenge.Kind.ToString() })
            });
        }

        if (_dbContext.SupportsAtomicOperations)
            await _dbContext.ExecuteInTransactionAsync(CompleteAsync, cancellationToken);
        else
        {
            await CompleteAsync(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new PasswordResetCompleteResponseDto();
    }

    private static BusinessRuleException ResetError(string message, string code) => new(message, code);
}

internal static class PasswordResetSecurity
{
    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public static string HashOtp(string code, string hashKey)
    {
        if (string.IsNullOrWhiteSpace(hashKey))
            throw new InvalidOperationException("OTP security configuration is unavailable.");
        return Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(hashKey), Encoding.UTF8.GetBytes(code))).ToLowerInvariant();
    }

    public static bool FixedTimeEquals(string expectedHex, string actualHex)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expectedHex), Convert.FromHexString(actualHex));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static IPAddress ParseIp(string? value) =>
        IPAddress.TryParse(value, out var parsed) ? parsed : IPAddress.Loopback;
}
