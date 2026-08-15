using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Application.Identity.Commands.ActivateTenantAccount;

public class ActivateTenantAccountCommandHandler : IRequestHandler<ActivateTenantAccountCommand, LoginResponseDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthService _authService;

    public ActivateTenantAccountCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAuthService authService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task<LoginResponseDto> Handle(ActivateTenantAccountCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ActivationToken))
            throw new ArgumentException("Activation token is required.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");

        var hashedToken = HashToken(request.ActivationToken.Trim());

        // 1. Query User by activation token hash
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == hashedToken && u.DeletedAt == null, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Activation token is invalid or has already been used.");
        }

        if (user.PasswordResetExpiresAt.HasValue && user.PasswordResetExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Activation token has expired.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Associated user account is deactivated.");
        }

        // 2. Hash password with Argon2id and clear activation token (one-time use)
        user.PasswordHash = _passwordHasher.HashPassword(request.Password);
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // 3. Update UserCompanyRole status to Active
        var memberships = await _dbContext.UserCompanyRoles
            .Where(ucr => ucr.UserId == user.Id && ucr.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var membership in memberships)
        {
            if (membership.Status == MembershipStatus.InvitedPending)
            {
                membership.Status = MembershipStatus.Active;
                membership.JoinedAt = DateTimeOffset.UtcNow;
                membership.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Authenticate session & issue JWT + refresh token via AuthService
        var loginDto = new LoginRequestDto
        {
            EmailOrPhone = user.Phone ?? user.Email ?? string.Empty,
            Password = request.Password
        };

        return await _authService.LoginAsync(loginDto, ipAddress: null, userAgent: "TenantActivation", cancellationToken);
    }

    private static string HashToken(string token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(token.Trim());
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
