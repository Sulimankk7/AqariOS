using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;

public class ProvisionTenantAccountCommandHandler : IRequestHandler<ProvisionTenantAccountCommand, ProvisionTenantAccountResponseDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IApplicationDbContext _dbContext;

    public ProvisionTenantAccountCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IApplicationDbContext dbContext)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ProvisionTenantAccountResponseDto> Handle(ProvisionTenantAccountCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        // 1. Fetch Tenant aggregate within Company boundary
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null || tenant.CompanyId != companyId)
        {
            throw new NotFoundException($"Tenant with ID {request.TenantId} was not found.");
        }

        // 2. Duplicate Account Check
        if (tenant.UserId.HasValue)
        {
            throw new ConflictException("TENANT_ACCOUNT_ALREADY_EXISTS: Tenant account has already been provisioned.");
        }

        // 3. Normalize Email & Phone
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        var phone = tenant.Phone.Trim();

        // 4. Email & Phone Collision Checks against Users
        if (!string.IsNullOrEmpty(email))
        {
            var emailExists = await _dbContext.Users
                .AnyAsync(u => u.Email != null && u.Email.ToLower() == email && u.DeletedAt == null, cancellationToken);
            if (emailExists)
            {
                throw new ConflictException($"A user account with email '{email}' already exists.");
            }
        }

        var phoneExists = await _dbContext.Users
            .AnyAsync(u => u.Phone != null && u.Phone == phone && u.DeletedAt == null, cancellationToken);
        if (phoneExists)
        {
            throw new ConflictException($"A user account with phone '{phone}' already exists.");
        }

        // 5. Generate 32-byte cryptographically secure activation token
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
        string rawToken = Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        string tokenHash = HashToken(rawToken);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(48);

        // 6. Create User Entity
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            FullName = tenant.Name,
            Phone = phone,
            Email = email,
            PasswordHash = null, // Set upon activation
            PasswordAlgorithm = "argon2id",
            PreferredLanguage = "ar",
            IsActive = true,
            PasswordResetTokenHash = tokenHash,
            PasswordResetExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Users.Add(user);

        // 7. Find System TENANT Role
        var tenantRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Code == "TENANT" && r.IsSystem && r.CompanyId == null && r.DeletedAt == null, cancellationToken);

        if (tenantRole == null)
        {
            // Seed on demand if seeder hasn't run yet
            tenantRole = new Role
            {
                Id = Guid.CreateVersion7(),
                Code = "TENANT",
                NameEn = "Tenant",
                NameAr = "مستأجر",
                IsSystem = true,
                CompanyId = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.Roles.Add(tenantRole);
        }

        // 8. Assign UserCompanyRole
        var membership = new UserCompanyRole
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            CompanyId = companyId,
            RoleId = tenantRole.Id,
            Status = MembershipStatus.InvitedPending,
            InvitedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.UserCompanyRoles.Add(membership);

        // 9. Link Tenant.UserId
        tenant.LinkUser(user.Id);

        // 10. Persist changes to database context
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProvisionTenantAccountResponseDto
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            CompanyId = companyId,
            ActivationToken = rawToken,
            ExpiresAt = expiresAt
        };
    }

    private static string HashToken(string token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(token.Trim());
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
