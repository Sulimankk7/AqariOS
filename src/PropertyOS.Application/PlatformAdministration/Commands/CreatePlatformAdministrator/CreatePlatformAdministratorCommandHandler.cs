using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.Identity;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Application.PlatformAdministration.Commands.CreatePlatformAdministrator;

public sealed class CreatePlatformAdministratorCommandHandler
    : IRequestHandler<CreatePlatformAdministratorCommand, PlatformAdministratorDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IBusinessClock _clock;

    public CreatePlatformAdministratorCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUser,
        ITenantContext tenantContext,
        IPasswordHasher passwordHasher,
        IBusinessClock clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<PlatformAdministratorDto> Handle(
        CreatePlatformAdministratorCommand request,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsPlatformAdmin)
            throw new UnauthorizedAccessException("Platform administrator scope is required.");

        var creatorId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated platform administrator is required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (emailExists)
            throw new DuplicateEmailException(normalizedEmail);

        var systemAdminRoleId = await _dbContext.Roles
            .Where(role =>
                role.Code == PlatformRoles.SystemAdmin &&
                role.IsSystem &&
                role.CompanyId == null &&
                role.DeletedAt == null)
            .Select(role => role.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (systemAdminRoleId == Guid.Empty)
            throw new InvalidOperationException("The global SYSTEM_ADMIN role is not configured.");

        var now = _clock.UtcNow;
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            PasswordAlgorithm = "argon2id",
            PreferredLanguage = "ar",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Users.Add(user);
        _dbContext.UserSystemRoles.Add(new UserSystemRole
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            RoleId = systemAdminRoleId,
            GrantedAt = now,
            GrantedBy = creatorId
        });

        return new PlatformAdministratorDto(
            user.Id,
            user.FullName,
            user.Email,
            user.IsActive,
            user.CreatedAt);
    }
}
