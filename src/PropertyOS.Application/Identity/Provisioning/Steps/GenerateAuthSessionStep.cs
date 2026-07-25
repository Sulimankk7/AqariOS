using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

/// <summary>
/// Provisioning step 8: Generates JWT access token and active refresh token session, constructing the <see cref="RegisterResponseDto"/>.
/// </summary>
public class GenerateAuthSessionStep : ITenantProvisioningStep
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public GenerateAuthSessionStep(IApplicationDbContext dbContext, IJwtTokenGenerator jwtTokenGenerator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
    }

    public int Order => 80;

    public async Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        var user = context.User ?? throw new InvalidOperationException("User entity must be populated to generate auth session.");
        var company = context.Company ?? throw new InvalidOperationException("Company entity must be populated to generate auth session.");
        var adminRole = context.AdminRole ?? throw new InvalidOperationException("AdminRole entity must be populated to generate auth session.");
        var membership = context.UserCompanyRole ?? throw new InvalidOperationException("UserCompanyRole entity must be populated to generate auth session.");

        var roles = new List<string> { adminRole.Code };
        var permissions = new List<string>();

        var (accessToken, expiresIn) = _jwtTokenGenerator.GenerateAccessToken(user.Id, company.Id, roles, permissions);
        var rawRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var hashedRefreshToken = _jwtTokenGenerator.HashRefreshToken(rawRefreshToken);

        var ipAddress = ParseIpAddress(context.Command.IpAddress);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IpAddress = ipAddress,
            UserAgent = context.Command.UserAgent,
            IssuedAt = context.CreatedAt
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        context.RefreshTokenEntity = refreshTokenEntity;

        var userRoleDto = new UserCompanyRoleDto
        {
            Id = membership.Id,
            CompanyId = company.Id,
            RoleId = adminRole.Id,
            RoleCode = adminRole.Code,
            RoleName = adminRole.NameEn,
            Status = membership.Status.ToString()
        };

        var userProfileDto = new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Phone = user.Phone,
            FullName = user.FullName,
            PreferredLanguage = user.PreferredLanguage,
            MfaEnabled = user.MfaEnabled,
            ActiveCompanyId = company.Id,
            CompanyRoles = new List<UserCompanyRoleDto> { userRoleDto },
            Permissions = permissions
        };

        context.Response = new RegisterResponseDto
        {
            UserId = user.Id,
            CompanyId = company.Id,
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = userProfileDto
        };
    }

    private static IPAddress ParseIpAddress(string? ipString)
    {
        if (string.IsNullOrWhiteSpace(ipString)) return IPAddress.Loopback;
        return IPAddress.TryParse(ipString, out var ip) ? ip : IPAddress.Loopback;
    }
}
