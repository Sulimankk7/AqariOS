using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity;
using PropertyOS.Application.Identity.Commands.ActivateTenantAccount;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Identity;

public class ActivateTenantAccountCommandHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    private static string HashToken(string token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(token.Trim());
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public async Task Handle_ValidTokenAndPassword_ActivatesAccountAndReturnsLoginResponse()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var rawToken = "valid-raw-activation-token";
        var hashedToken = HashToken(rawToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Tariq Aziz",
            Phone = "+962791118888",
            PasswordHash = null,
            PasswordResetTokenHash = hashedToken,
            PasswordResetExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            IsActive = true
        };
        dbContext.Users.Add(user);

        var membership = new UserCompanyRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CompanyId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            Status = MembershipStatus.InvitedPending
        };
        dbContext.UserCompanyRoles.Add(membership);
        await dbContext.SaveChangesAsync();

        var passwordHasherMock = Substitute.For<IPasswordHasher>();
        passwordHasherMock.HashPassword("NewPassword123!").Returns("hashed-password-argon2id");

        var authServiceMock = Substitute.For<IAuthService>();
        var expectedResponse = new LoginResponseDto
        {
            AccessToken = "jwt-tenant-token",
            RefreshToken = "raw-refresh-token",
            ExpiresIn = 900
        };
        authServiceMock.LoginAsync(Arg.Any<LoginRequestDto>(), null, "TenantActivation", Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var handler = new ActivateTenantAccountCommandHandler(dbContext, passwordHasherMock, authServiceMock);
        var command = new ActivateTenantAccountCommand(rawToken, "NewPassword123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);

        // Verify user password updated and activation token cleared (one-time token semantics)
        user.PasswordHash.Should().Be("hashed-password-argon2id");
        user.PasswordResetTokenHash.Should().BeNull("token hash must be cleared upon successful activation");
        user.PasswordResetExpiresAt.Should().BeNull("token expiry must be cleared upon successful activation");

        // Verify membership status active
        membership.Status.Should().Be(MembershipStatus.Active);
        membership.JoinedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReusingConsumedToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var rawToken = "already-used-token";
        var hashedToken = HashToken(rawToken);

        // User already completed activation: PasswordHash is set, PasswordResetTokenHash is null
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Active User",
            Phone = "+962791118888",
            PasswordHash = "already-hashed-password",
            PasswordResetTokenHash = null,
            PasswordResetExpiresAt = null,
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var passwordHasherMock = Substitute.For<IPasswordHasher>();
        var authServiceMock = Substitute.For<IAuthService>();
        var handler = new ActivateTenantAccountCommandHandler(dbContext, passwordHasherMock, authServiceMock);
        var command = new ActivateTenantAccountCommand(rawToken, "NewPassword123!");

        // Act & Assert
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*invalid or has already been used*");

        // Ensure password was NOT modified
        user.PasswordHash.Should().Be("already-hashed-password");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var rawToken = "expired-token";
        var hashedToken = HashToken(rawToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Expired User",
            PasswordResetTokenHash = hashedToken,
            PasswordResetExpiresAt = DateTimeOffset.UtcNow.AddHours(-1), // Expired
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var passwordHasherMock = Substitute.For<IPasswordHasher>();
        var authServiceMock = Substitute.For<IAuthService>();
        var handler = new ActivateTenantAccountCommandHandler(dbContext, passwordHasherMock, authServiceMock);

        var command = new ActivateTenantAccountCommand(rawToken, "NewPassword123!");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var passwordHasherMock = Substitute.For<IPasswordHasher>();
        var authServiceMock = Substitute.For<IAuthService>();
        var handler = new ActivateTenantAccountCommandHandler(dbContext, passwordHasherMock, authServiceMock);

        var command = new ActivateTenantAccountCommand("invalid-token", "NewPassword123!");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*invalid or has already been used*");
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var rawToken = "deactivated-user-token";
        var hashedToken = HashToken(rawToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Deactivated User",
            PasswordResetTokenHash = hashedToken,
            PasswordResetExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            IsActive = false
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var passwordHasherMock = Substitute.For<IPasswordHasher>();
        var authServiceMock = Substitute.For<IAuthService>();
        var handler = new ActivateTenantAccountCommandHandler(dbContext, passwordHasherMock, authServiceMock);

        var command = new ActivateTenantAccountCommand(rawToken, "NewPassword123!");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*deactivated*");
    }
}
