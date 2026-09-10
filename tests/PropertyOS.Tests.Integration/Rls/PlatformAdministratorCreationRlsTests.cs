using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Security;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Infrastructure.Persistence.Interceptors;
using PropertyOS.Tests.Integration.Infrastructure;

namespace PropertyOS.Tests.Integration.Rls;

[Collection("Postgres collection")]
public sealed class PlatformAdministratorCreationRlsTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;

    public PlatformAdministratorCreationRlsTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    [Fact]
    public async Task PlatformContext_CanAtomicallyCreateUserWithSystemAdminRole()
    {
        var (creatorId, roleId) = await SeedCreatorAndSystemRoleAsync();
        var userId = Guid.CreateVersion7();
        await using var appContext = CreateRestrictedContext(
            companyId: null,
            isPlatformAdmin: true,
            userId: creatorId);

        await using (var transaction = await appContext.Database.BeginTransactionAsync())
        {
            (await ReadSettingAsync(appContext, "app.current_user_id"))
                .Should().Be(creatorId.ToString("D"));
            (await ReadSettingAsync(appContext, "app.is_platform_admin"))
                .Should().Be("true");
            (await ReadSettingAsync(appContext, "app.current_company_id"))
                .Should().BeNull();

            appContext.Users.Add(CreateUser(userId, "created-admin@example.com"));
            appContext.UserSystemRoles.Add(new UserSystemRole
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                RoleId = roleId,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedBy = creatorId
            });

            await appContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        _fixture.Context.ChangeTracker.Clear();
        (await _fixture.Context.Users.IgnoreQueryFilters().AnyAsync(user => user.Id == userId)).Should().BeTrue();
        (await _fixture.Context.UserSystemRoles.CountAsync(assignment => assignment.UserId == userId)).Should().Be(1);
        (await _fixture.Context.UserCompanyRoles.CountAsync(membership => membership.UserId == userId)).Should().Be(0);
    }

    [Fact]
    public async Task PlatformContext_GrantedByMismatch_IsRejected()
    {
        var (creatorId, roleId) = await SeedCreatorAndSystemRoleAsync();
        var otherUserId = Guid.CreateVersion7();
        _fixture.Context.Users.Add(CreateUser(otherUserId, $"other-{otherUserId}@example.com"));
        await _fixture.Context.SaveChangesAsync();
        var userId = Guid.CreateVersion7();

        await using var transaction = await _fixture.AppUserContext.Database.BeginTransactionAsync();
        await SetPlatformContextAsync(_fixture.AppUserContext, creatorId);
        _fixture.AppUserContext.Users.Add(CreateUser(userId, "wrong-grantor@example.com"));
        _fixture.AppUserContext.UserSystemRoles.Add(new UserSystemRole
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            RoleId = roleId,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedBy = otherUserId
        });

        var action = () => _fixture.AppUserContext.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateException>();
        await transaction.RollbackAsync();

        _fixture.Context.ChangeTracker.Clear();
        (await _fixture.Context.Users.IgnoreQueryFilters().AnyAsync(user => user.Id == userId))
            .Should().BeFalse();
    }

    [Fact]
    public async Task PlatformContext_CannotAssignArbitraryGlobalRole()
    {
        var (creatorId, _) = await SeedCreatorAndSystemRoleAsync();
        var otherRole = new Role
        {
            Id = Guid.CreateVersion7(),
            Code = "OTHER_SYSTEM_ROLE",
            NameEn = "Other",
            NameAr = "آخر",
            IsSystem = true,
            CompanyId = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _fixture.Context.Roles.Add(otherRole);
        await _fixture.Context.SaveChangesAsync();
        var userId = Guid.CreateVersion7();

        await using var transaction = await _fixture.AppUserContext.Database.BeginTransactionAsync();
        await SetPlatformContextAsync(_fixture.AppUserContext, creatorId);
        _fixture.AppUserContext.Users.Add(CreateUser(userId, "blocked-admin@example.com"));
        _fixture.AppUserContext.UserSystemRoles.Add(new UserSystemRole
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            RoleId = otherRole.Id,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedBy = creatorId
        });

        var action = () => _fixture.AppUserContext.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateException>();
        await transaction.RollbackAsync();

        _fixture.Context.ChangeTracker.Clear();
        (await _fixture.Context.Users.IgnoreQueryFilters().AnyAsync(user => user.Id == userId)).Should().BeFalse();
    }

    [Fact]
    public async Task MissingPlatformContext_CannotInsertUser()
    {
        var userId = Guid.CreateVersion7();
        await using var transaction = await _fixture.AppUserContext.Database.BeginTransactionAsync();
        _fixture.AppUserContext.Users.Add(CreateUser(userId, "blocked-without-scope@example.com"));

        var action = () => _fixture.AppUserContext.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateException>();
        await transaction.RollbackAsync();

        _fixture.Context.ChangeTracker.Clear();
        (await _fixture.Context.Users.IgnoreQueryFilters().AnyAsync(user => user.Id == userId)).Should().BeFalse();
    }

    private async Task<(Guid CreatorId, Guid SystemRoleId)> SeedCreatorAndSystemRoleAsync()
    {
        var creatorId = Guid.CreateVersion7();
        var systemRole = new Role
        {
            Id = Guid.CreateVersion7(),
            Code = PlatformRoles.SystemAdmin,
            NameEn = "System Administrator",
            NameAr = "مدير النظام",
            IsSystem = true,
            CompanyId = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _fixture.Context.Roles.Add(systemRole);
        _fixture.Context.Users.Add(CreateUser(creatorId, $"creator-{creatorId}@example.com"));
        await _fixture.Context.SaveChangesAsync();
        return (creatorId, systemRole.Id);
    }

    private static User CreateUser(Guid id, string email) => new()
    {
        Id = id,
        FullName = "Platform Administrator",
        Email = email,
        PasswordHash = "argon2id.test.hash",
        PasswordAlgorithm = "argon2id",
        PreferredLanguage = "ar",
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static async Task SetPlatformContextAsync(PropertyOsDbContext context, Guid userId)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_user_id', {userId.ToString()}, true)");
        await context.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.is_platform_admin', 'true', true)");
        await context.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_company_id', '', true)");
    }

    private PropertyOsDbContext CreateRestrictedContext(
        Guid? companyId,
        bool isPlatformAdmin,
        Guid? userId)
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(_fixture.AppUserDataSource!)
            .AddInterceptors(new TenantSessionInterceptor(
                new TestTenantContext(companyId, isPlatformAdmin),
                new TestCurrentUserContext(userId)))
            .Options;

        return new PropertyOsDbContext(options);
    }

    private static async Task<string?> ReadSettingAsync(
        PropertyOsDbContext context,
        string settingName)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)context.Database.CurrentTransaction!
            .GetDbTransaction();
        command.CommandText = $"SELECT NULLIF(current_setting('{settingName}', true), '')";
        return await command.ExecuteScalarAsync() as string;
    }

    private sealed record TestTenantContext(Guid? CompanyId, bool IsPlatformAdmin)
        : ITenantContext;

    private sealed record TestCurrentUserContext(Guid? UserId)
        : ICurrentUserContext;
}
