using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Identity;

/// <summary>
/// Infrastructure authentication service implementing user login, token refresh rotation,
/// theft detection, logout session management, and OTP verification.
/// </summary>
public class AuthService : IAuthService
{
    private readonly PropertyOsDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthService>? _logger;
    private readonly IHostEnvironment? _environment;
    private readonly ISmsSender? _smsSender;
    private readonly OtpOptions _otpOptions;
    private readonly RefreshSessionOptions _refreshSessionOptions;

    /// <summary>
    /// Initializes a new instance of AuthService.
    /// </summary>
    public AuthService(
        PropertyOsDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthService>? logger = null,
        IHostEnvironment? environment = null,
        ISmsSender? smsSender = null,
        IOptions<OtpOptions>? otpOptions = null,
        IOptions<RefreshSessionOptions>? refreshSessionOptions = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
        _logger = logger;
        _environment = environment;
        _smsSender = smsSender;
        _otpOptions = otpOptions?.Value ?? new OtpOptions { HashKey = "unit-test-only-otp-hash-key" };
        _refreshSessionOptions = refreshSessionOptions?.Value ?? new RefreshSessionOptions();
    }

    /// <inheritdoc />
    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto dto,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var input = dto.EmailOrPhone.Trim();
        var isEmail = input.Contains('@');

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => isEmail
                ? u.Email != null && u.Email.ToLower() == input.ToLower()
                : u.Phone != null && u.Phone == input, cancellationToken);

        if (user == null || user.DeletedAt != null)
        {
            await RecordLoginHistoryAsync(user?.Id, input, ipAddress, userAgent, LoginStatus.FailedNotFound, cancellationToken);
            throw new UnauthorizedAccessException("Invalid email/phone or password.");
        }

        if (!user.IsActive)
        {
            await RecordLoginHistoryAsync(user.Id, input, ipAddress, userAgent, LoginStatus.FailedNotFound, cancellationToken);
            throw new BusinessRuleException("This account is not currently available.", "ACCOUNT_NOT_ACTIVE");
        }

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTimeOffset.UtcNow)
        {
            await RecordLoginHistoryAsync(user.Id, input, ipAddress, userAgent, LoginStatus.FailedLocked, cancellationToken);
            throw new UnauthorizedAccessException("Account is temporarily locked due to multiple failed attempts. Please try again later.");
        }

        bool isPasswordValid = false;
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            isPasswordValid = _passwordHasher.VerifyPassword(dto.Password, user.PasswordHash);
        }

        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(15);
            }
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await RecordLoginHistoryAsync(user.Id, input, ipAddress, userAgent, LoginStatus.FailedPassword, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedAccessException("Invalid email/phone or password.");
        }

        // Login Succeeded: Reset failed counters
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await RecordLoginHistoryAsync(user.Id, input, ipAddress, userAgent, LoginStatus.Success, cancellationToken);

        var (profile, companyId, roles, permissions) = await LoadUserProfileAndClaimsAsync(user.Id, cancellationToken);

        var (accessToken, expiresIn) = _jwtTokenGenerator.GenerateAccessToken(user.Id, companyId, roles, permissions);

        var now = DateTimeOffset.UtcNow;
        var rawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _jwtTokenGenerator.HashRefreshToken(rawRefreshToken);
        var isRememberedSession = dto.RememberMe == true;
        var isPersistentSession = dto.RememberMe != false;
        var absoluteSessionExpiresAt = isRememberedSession
            ? now.AddDays(_refreshSessionOptions.RememberedLifetimeDays)
            : (DateTimeOffset?)null;
        var refreshTokenExpiresAt = absoluteSessionExpiresAt
            ?? now.AddDays(_refreshSessionOptions.NormalLifetimeDays);

        var familyId = Guid.NewGuid();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            FamilyId = familyId,
            ExpiresAt = refreshTokenExpiresAt,
            IsPersistent = isPersistentSession,
            AbsoluteSessionExpiresAt = absoluteSessionExpiresAt,
            IpAddress = ParseIpAddress(ipAddress),
            UserAgent = userAgent,
            IssuedAt = now
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            IsPersistentSession = refreshTokenEntity.IsPersistent,
            RefreshTokenExpiresAt = refreshTokenEntity.ExpiresAt,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = profile
        };
    }

    /// <inheritdoc />
    public async Task<LoginResponseDto> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token is required.");
        }

        var hashedToken = _jwtTokenGenerator.HashRefreshToken(refreshToken);

        // ROTATION RACE PROTECTION:
        // Concurrent refreshes presenting the SAME token must serialize. We open an explicit
        // transaction and acquire a row-level lock (SELECT ... FOR UPDATE) on the refresh-token
        // row before validating/rotating it (mirrors EfawateercomTransactionRepository.
        // GetByIdForUpdateAsync — RefreshToken does not map xmin, so no ", xmin" projection).
        // The losing caller blocks on the lock until the winner commits, then re-reads the row
        // and observes RevokedAt set — which routes it into the reuse/theft-detection path below.
        // The in-memory provider (unit tests) supports neither transactions nor FromSqlRaw,
        // so both are applied only when the underlying provider is relational.
        var isRelational = _dbContext.Database.IsRelational();
        await using var transaction = isRelational
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var tokenEntity = isRelational
            ? await _dbContext.RefreshTokens
                .FromSqlRaw("SELECT * FROM refresh_tokens WHERE token_hash = {0} FOR UPDATE", hashedToken)
                .FirstOrDefaultAsync(cancellationToken)
            : await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken, cancellationToken);

        if (tokenEntity == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        // REUSE DETECTION / THEFT PREVENTION:
        // If a revoked token in a family is presented again, revoke all tokens in the entire family!
        if (tokenEntity.RevokedAt.HasValue)
        {
            var familyTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.FamilyId == tokenEntity.FamilyId && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var t in familyTokens)
            {
                t.RevokedAt = DateTimeOffset.UtcNow;
                t.RevokedReason = RevokeReason.TheftDetected;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // The family revocation must survive the throw below — commit before throwing.
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            throw new UnauthorizedAccessException("Session revoked due to security violation. Please log in again.");
        }

        var now = DateTimeOffset.UtcNow;
        if (tokenEntity.ExpiresAt <= now ||
            (tokenEntity.AbsoluteSessionExpiresAt.HasValue && tokenEntity.AbsoluteSessionExpiresAt.Value <= now))
        {
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == tokenEntity.UserId && u.IsActive && u.DeletedAt == null, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Associated user account is no longer active.");
        }

        // Rotate token
        var newRawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var newHashedRefreshToken = _jwtTokenGenerator.HashRefreshToken(newRawRefreshToken);

        var newTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newHashedRefreshToken,
            FamilyId = tokenEntity.FamilyId,
            ExpiresAt = tokenEntity.AbsoluteSessionExpiresAt
                ?? now.AddDays(_refreshSessionOptions.NormalLifetimeDays),
            IsPersistent = tokenEntity.IsPersistent,
            AbsoluteSessionExpiresAt = tokenEntity.AbsoluteSessionExpiresAt,
            IpAddress = ParseIpAddress(ipAddress),
            IssuedAt = now
        };

        tokenEntity.RevokedAt = DateTimeOffset.UtcNow;
        tokenEntity.RevokedReason = RevokeReason.Rotated;
        tokenEntity.ReplacedByToken = newTokenEntity;
        tokenEntity.ReplacedByTokenId = newTokenEntity.Id;

        _dbContext.RefreshTokens.Add(newTokenEntity);

        var (profile, companyId, roles, permissions) = await LoadUserProfileAndClaimsAsync(user.Id, cancellationToken);
        var (accessToken, expiresIn) = _jwtTokenGenerator.GenerateAccessToken(user.Id, companyId, roles, permissions);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRawRefreshToken,
            IsPersistentSession = newTokenEntity.IsPersistent,
            RefreshTokenExpiresAt = newTokenEntity.ExpiresAt,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = profile
        };
    }

    /// <inheritdoc />
    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var hashedToken = _jwtTokenGenerator.HashRefreshToken(refreshToken);
        var tokenEntity = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken && rt.RevokedAt == null, cancellationToken);

        if (tokenEntity != null)
        {
            tokenEntity.RevokedAt = DateTimeOffset.UtcNow;
            tokenEntity.RevokedReason = RevokeReason.Logout;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task LogoutAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedReason = RevokeReason.AdminRevoked;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> RequestOtpAsync(OtpRequestDto dto, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        if (!OtpPhoneNumber.TryNormalize(dto.Phone, out var phone, allowJordanianLocal: true))
            throw new ArgumentException("Phone number must be a valid international E.164 number.", nameof(dto));
        if (dto.Purpose != OtpPurpose.Login)
            throw new InvalidOperationException("This verification purpose is not available.");
        if (string.IsNullOrWhiteSpace(_otpOptions.HashKey))
            throw new InvalidOperationException("OTP security configuration is unavailable.");

        var eligibleUserExists = await _dbContext.Users.AnyAsync(
            u => u.Phone == phone && u.IsActive && u.DeletedAt == null,
            cancellationToken);
        if (!eligibleUserExists)
        {
            _logger?.LogInformation(
                "OTP request rejected because no active account matches. Destination={DestinationMasked}",
                MaskPhone(phone));
            throw new NotFoundException("No account is registered with this phone number.");
        }

        var now = DateTimeOffset.UtcNow;
        var latest = await _dbContext.OtpChallenges
            .Where(c => c.Phone == phone && c.Purpose == dto.Purpose)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (latest != null)
        {
            var retryAfter = _otpOptions.ResendCooldownSeconds - (int)(now - latest.CreatedAt).TotalSeconds;
            if (retryAfter > 0) throw new OtpRequestThrottledException(retryAfter);
        }

        var windowStart = now.AddMinutes(-_otpOptions.DestinationWindowMinutes);
        var destinationRequestCount = await _dbContext.OtpChallenges.CountAsync(
            c => c.Phone == phone && c.Purpose == dto.Purpose && c.CreatedAt >= windowStart, cancellationToken);
        if (destinationRequestCount >= _otpOptions.MaxRequestsPerDestinationWindow)
            throw new OtpRequestThrottledException(_otpOptions.DestinationWindowMinutes * 60);

        // GetInt32 is CSPRNG-backed; upper bound is exclusive, and D6 preserves leading zeroes.
        string rawCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        string hashedCode = HashOtpCode(rawCode);

        var challenge = new OtpChallenge
        {
            Phone = phone,
            CodeHash = hashedCode,
            Purpose = dto.Purpose,
            ExpiresAt = now.AddMinutes(_otpOptions.ExpiryMinutes),
            ConsumedAt = null,
            FailedAttempts = 0,
            MaxAttempts = 3,
            RequestedIp = ParseIpAddress(clientIp),
            CreatedAt = now
        };

        // Historical rows stay intact but old active codes become immediately unusable.
        var activeChallenges = _dbContext.OtpChallenges
            .Where(c => c.Phone == phone && c.Purpose == dto.Purpose && c.ConsumedAt == null && c.ExpiresAt > now);
        if (_dbContext.Database.IsRelational())
            await activeChallenges.ExecuteUpdateAsync(s => s.SetProperty(c => c.ConsumedAt, now), cancellationToken);
        else
        {
            foreach (var active in await activeChallenges.ToListAsync(cancellationToken)) active.ConsumedAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        _dbContext.OtpChallenges.Add(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var message = $"رمز التحقق الخاص بك في AqariOS هو: {rawCode}\nالرمز صالح لمدة {_otpOptions.ExpiryMinutes} دقائق.";
        // Unit tests use the constructor without DI. Runtime DI always supplies ISmsSender.
        var sent = _smsSender == null ? _environment == null : await _smsSender.SendAsync(phone, message, cancellationToken);
        if (!sent)
        {
            challenge.ConsumedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger?.LogWarning("OTP delivery failed. Destination={DestinationMasked}", MaskPhone(phone));
            throw new OtpDeliveryException();
        }

        _logger?.LogInformation("OTP challenge delivered. Destination={DestinationMasked}", MaskPhone(phone));
        // The API deliberately discards this value; retaining the interface return preserves
        // existing internal/test callers without exposing the code to clients.
        return rawCode;
    }

    /// <inheritdoc />
    public async Task<LoginResponseDto> VerifyOtpAsync(
        OtpVerifyDto dto,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        if (!OtpPhoneNumber.TryNormalize(dto.Phone, out var phone))
            throw new UnauthorizedAccessException("OTP code is invalid or has expired.");
        if (dto.Purpose != OtpPurpose.Login)
            throw new InvalidOperationException("This verification purpose is not available.");
        var hashedCode = HashOtpCode(dto.Code.Trim());

        var challenge = await _dbContext.OtpChallenges
            .Where(c => c.Phone == phone && c.Purpose == dto.Purpose && c.ConsumedAt == null && c.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge == null)
        {
            throw new UnauthorizedAccessException("OTP code is invalid or has expired.");
        }

        if (challenge.FailedAttempts >= challenge.MaxAttempts)
        {
            throw new UnauthorizedAccessException("OTP verification attempts exceeded. Please request a new OTP.");
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(challenge.CodeHash), Convert.FromHexString(hashedCode)))
        {
            if (_dbContext.Database.IsRelational())
                await _dbContext.OtpChallenges.Where(c => c.Id == challenge.Id && c.FailedAttempts < c.MaxAttempts)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.FailedAttempts, c => (short)(c.FailedAttempts + 1)), cancellationToken);
            else
            {
                challenge.FailedAttempts++;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            throw new UnauthorizedAccessException("OTP code is invalid.");
        }

        // A single conditional SQL update is the consumption gate: at most one request can win.
        var consumed = 0;
        if (_dbContext.Database.IsRelational())
            consumed = await _dbContext.OtpChallenges
                .Where(c => c.Id == challenge.Id && c.ConsumedAt == null && c.ExpiresAt > DateTimeOffset.UtcNow && c.FailedAttempts < c.MaxAttempts)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ConsumedAt, DateTimeOffset.UtcNow)
                    .SetProperty(c => c.VerifiedIp, ParseIpAddress(ipAddress)), cancellationToken);
        else if (challenge.ConsumedAt == null && challenge.ExpiresAt > DateTimeOffset.UtcNow && challenge.FailedAttempts < challenge.MaxAttempts)
        {
            challenge.ConsumedAt = DateTimeOffset.UtcNow;
            challenge.VerifiedIp = ParseIpAddress(ipAddress);
            await _dbContext.SaveChangesAsync(cancellationToken);
            consumed = 1;
        }
        if (consumed != 1)
            throw new UnauthorizedAccessException("OTP code is invalid or has expired.");

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Phone == phone && u.IsActive && u.DeletedAt == null, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with phone '{phone}' was not found.");
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await RecordLoginHistoryAsync(user.Id, phone, ipAddress, userAgent, LoginStatus.Success, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var (profile, companyId, roles, permissions) = await LoadUserProfileAndClaimsAsync(user.Id, cancellationToken);
        var (accessToken, expiresIn) = _jwtTokenGenerator.GenerateAccessToken(user.Id, companyId, roles, permissions);

        var refreshIssuedAt = DateTimeOffset.UtcNow;
        var rawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _jwtTokenGenerator.HashRefreshToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            FamilyId = Guid.NewGuid(),
            ExpiresAt = refreshIssuedAt.AddDays(_refreshSessionOptions.NormalLifetimeDays),
            IsPersistent = true,
            AbsoluteSessionExpiresAt = null,
            IpAddress = ParseIpAddress(ipAddress),
            IssuedAt = refreshIssuedAt
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            IsPersistentSession = refreshTokenEntity.IsPersistent,
            RefreshTokenExpiresAt = refreshTokenEntity.ExpiresAt,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = profile
        };
    }

    /// <inheritdoc />
    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' was not found.");
        }

        var (profile, _, _, _) = await LoadUserProfileAndClaimsAsync(user.Id, cancellationToken);
        return profile;
    }

    private async Task<(UserProfileDto Profile, Guid? ActiveCompanyId, List<string> Roles, List<string> Permissions)> LoadUserProfileAndClaimsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        var companyRoles = await _dbContext.UserCompanyRoles
            .AsNoTracking()
            .Where(ucr => ucr.UserId == userId &&
                          ucr.DeletedAt == null &&
                          ucr.Status == MembershipStatus.Active &&
                          ucr.Company.IsActive &&
                          ucr.Company.DeletedAt == null)
            .Select(ucr => new
            {
                ucr.Id,
                ucr.CompanyId,
                ucr.RoleId,
                RoleCode = ucr.Role.Code,
                RoleName = ucr.Role.NameEn,
                Status = ucr.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        var activeCompanyId = companyRoles.Select(cr => (Guid?)cr.CompanyId).FirstOrDefault();

        var systemRoles = await _dbContext.UserSystemRoles
            .AsNoTracking()
            .Where(usr => usr.UserId == userId && usr.Role.IsSystem && usr.Role.CompanyId == null && usr.Role.DeletedAt == null)
            .Select(usr => new { usr.RoleId, usr.Role.Code })
            .ToListAsync(cancellationToken);

        var roleIds = companyRoles.Select(cr => cr.RoleId)
            .Concat(systemRoles.Select(sr => sr.RoleId))
            .Distinct()
            .ToList();
        var roleCodes = companyRoles.Select(cr => cr.RoleCode)
            .Concat(systemRoles.Select(sr => sr.Code))
            .Distinct()
            .ToList();

        var permissionKeys = await (from rp in _dbContext.RolePermissions.AsNoTracking()
                                   join p in _dbContext.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                                   where roleIds.Contains(rp.RoleId)
                                   select p.Key)
                                  .Distinct()
                                  .ToListAsync(cancellationToken);

        var companyRoleDtos = companyRoles.Select(cr => new UserCompanyRoleDto
        {
            Id = cr.Id,
            CompanyId = cr.CompanyId,
            RoleId = cr.RoleId,
            RoleCode = cr.RoleCode,
            RoleName = cr.RoleName,
            Status = cr.Status
        }).ToList();

        var profile = new UserProfileDto
        {
            Id = userId,
            Email = user?.Email,
            Phone = user?.Phone,
            FullName = user?.FullName ?? string.Empty,
            PreferredLanguage = user?.PreferredLanguage ?? "ar",
            MfaEnabled = user?.MfaEnabled ?? false,
            ActiveCompanyId = activeCompanyId,
            CompanyRoles = companyRoleDtos,
            Permissions = permissionKeys,
            SystemRoles = systemRoles.Select(sr => sr.Code).Distinct().ToList()
        };

        return (profile, activeCompanyId, roleCodes, permissionKeys);
    }

    private async Task RecordLoginHistoryAsync(
        Guid? userId,
        string attemptedIdentifier,
        string? ipAddress,
        string? userAgent,
        LoginStatus status,
        CancellationToken cancellationToken)
    {
        var history = new LoginHistory
        {
            UserId = userId,
            AttemptedIdentifier = attemptedIdentifier,
            IpAddress = ParseIpAddress(ipAddress),
            UserAgent = userAgent,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.LoginHistory.Add(history);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Logging history should not block auth flow
        }
    }

    private static System.Net.IPAddress ParseIpAddress(string? ipString)
    {
        if (string.IsNullOrWhiteSpace(ipString)) return System.Net.IPAddress.Loopback;
        return System.Net.IPAddress.TryParse(ipString, out var ip) ? ip : System.Net.IPAddress.Loopback;
    }

    private string HashOtpCode(string code)
    {
        byte[] key = Encoding.UTF8.GetBytes(_otpOptions.HashKey);
        byte[] bytes = Encoding.UTF8.GetBytes(code);
        byte[] hash = HMACSHA256.HashData(key, bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string MaskPhone(string phone)
        => phone.Length <= 7 ? "***" : $"{phone[..4]}****{phone[^3..]}";
}
