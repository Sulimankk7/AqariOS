using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity;
using PropertyOS.Application.PlatformAdministration.Commands.CreatePlatformAdministrator;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Audit.Attributes;
using PropertyOS.Infrastructure.Identity;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Tests.Unit.Application.PlatformAdministration;

public sealed class CreatePlatformAdministratorCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesActiveGlobalAdministrator()
    {
        await using var dbContext = CreateDbContext();
        var role = CreateSystemAdminRole();
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var creatorId = Guid.CreateVersion7();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.HashPassword("password123").Returns("argon2id.test.hash");
        var handler = CreateHandler(dbContext, creatorId, passwordHasher);

        var result = await handler.Handle(
            new CreatePlatformAdministratorCommand("  New Admin  ", "  ADMIN@EXAMPLE.COM ", "password123"),
            CancellationToken.None);

        var user = dbContext.ChangeTracker.Entries<User>().Single().Entity;
        var assignment = dbContext.ChangeTracker.Entries<UserSystemRole>().Single().Entity;

        result.Id.Should().Be(user.Id);
        user.FullName.Should().Be("New Admin");
        user.Email.Should().Be("admin@example.com");
        user.IsActive.Should().BeTrue();
        user.PasswordHash.Should().Be("argon2id.test.hash");
        user.PasswordHash.Should().NotBe("password123");
        assignment.UserId.Should().Be(user.Id);
        assignment.RoleId.Should().Be(role.Id);
        assignment.GrantedBy.Should().Be(creatorId);
        dbContext.UserCompanyRoles.Local.Should().BeEmpty();
        passwordHasher.Received(1).HashPassword("password123");
    }

    [Fact]
    public async Task Handle_DuplicateSoftDeletedEmail_ThrowsConflict()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Roles.Add(CreateSystemAdminRole());
        dbContext.Users.Add(new User
        {
            Id = Guid.CreateVersion7(),
            FullName = "Deleted User",
            Email = "existing@example.com",
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext, Guid.CreateVersion7(), Substitute.For<IPasswordHasher>());
        var action = () => handler.Handle(
            new CreatePlatformAdministratorCommand("New Admin", "EXISTING@example.com", "password123"),
            CancellationToken.None);

        await action.Should().ThrowAsync<DuplicateEmailException>();
        dbContext.ChangeTracker.Entries<UserSystemRole>().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NonPlatformContext_IsRejectedBeforeWriting()
    {
        await using var dbContext = CreateDbContext();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsPlatformAdmin.Returns(false);
        var handler = new CreatePlatformAdministratorCommandHandler(
            dbContext,
            Substitute.For<ICurrentUserContext>(),
            tenantContext,
            Substitute.For<IPasswordHasher>(),
            Substitute.For<IBusinessClock>());

        var action = () => handler.Handle(
            new CreatePlatformAdministratorCommand("New Admin", "admin@example.com", "password123"),
            CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
        dbContext.Users.Local.Should().BeEmpty();
        dbContext.UserSystemRoles.Local.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatedAdministrator_CanLoginAndProducesSystemAdminRoleClaimInput()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Roles.Add(CreateSystemAdminRole());
        await dbContext.SaveChangesAsync();
        var passwordHasher = new PasswordHasher();
        var handler = CreateHandler(dbContext, Guid.CreateVersion7(), passwordHasher);

        var created = await handler.Handle(
            new CreatePlatformAdministratorCommand("Login Admin", "login-admin@example.com", "password123"),
            CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var jwt = Substitute.For<IJwtTokenGenerator>();
        jwt.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
            .Returns(("access-token", 900));
        jwt.GenerateRefreshToken().Returns("refresh-token");
        jwt.HashRefreshToken("refresh-token").Returns("refresh-token-hash");
        var authService = new AuthService(dbContext, passwordHasher, jwt);

        var login = await authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = "login-admin@example.com",
            Password = "password123",
            RememberMe = false
        }, null, null);

        login.AccessToken.Should().Be("access-token");
        login.User.Id.Should().Be(created.Id);
        login.User.SystemRoles.Should().Contain(PlatformRoles.SystemAdmin);
        jwt.Received(1).GenerateAccessToken(
            created.Id,
            Arg.Is<Guid?>(companyId => companyId == null),
            Arg.Is<IEnumerable<string>>(roles => roles.Contains(PlatformRoles.SystemAdmin)),
            Arg.Any<IEnumerable<string>>());
    }

    [Theory]
    [InlineData("", "admin@example.com", "password123")]
    [InlineData("Admin", "invalid", "password123")]
    [InlineData("Admin", "admin@example.com", "short")]
    public void Validator_InvalidInput_IsRejected(string name, string email, string password)
    {
        var validator = new CreatePlatformAdministratorCommandValidator();

        validator.Validate(new CreatePlatformAdministratorCommand(name, email, password)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UserPasswordHash_IsMarkedSensitiveForExistingAuditRedaction()
    {
        typeof(User).GetProperty(nameof(User.PasswordHash))!
            .IsDefined(typeof(SensitiveAttribute), inherit: false)
            .Should().BeTrue();
    }

    private static PropertyOsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    private static Role CreateSystemAdminRole() => new()
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

    private static CreatePlatformAdministratorCommandHandler CreateHandler(
        PropertyOsDbContext dbContext,
        Guid creatorId,
        IPasswordHasher passwordHasher)
    {
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(creatorId);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsPlatformAdmin.Returns(true);
        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));

        return new CreatePlatformAdministratorCommandHandler(
            dbContext,
            currentUser,
            tenantContext,
            passwordHasher,
            clock);
    }
}
