using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.Identity;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Infrastructure.Identity;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Infrastructure.Identity;

public class RefreshTokenRotationTests
{
    private static PropertyOsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task RefreshTokenAsync_Should_Rotate_Token_Correctly_With_Valid_Replacement_Id_And_Navigation()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var jwtGeneratorMock = Substitute.For<IJwtTokenGenerator>();
        var passwordHasherMock = Substitute.For<IPasswordHasher>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "user@test.com",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.Users.Add(user);

        var rawToken = "raw_refresh_token_123";
        var hashedToken = "hashed_refresh_token_123";
        var familyId = Guid.NewGuid();
        var oldToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hashedToken,
            FamilyId = familyId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IpAddress = IPAddress.Loopback,
            IssuedAt = DateTimeOffset.UtcNow
        };
        dbContext.RefreshTokens.Add(oldToken);
        await dbContext.SaveChangesAsync();

        jwtGeneratorMock.HashRefreshToken(rawToken).Returns(hashedToken);
        jwtGeneratorMock.GenerateRefreshToken().Returns("new_raw_refresh_token_456");
        jwtGeneratorMock.HashRefreshToken("new_raw_refresh_token_456").Returns("new_hashed_refresh_token_456");
        jwtGeneratorMock.GenerateAccessToken(user.Id, Arg.Any<Guid?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
            .Returns(("access_token_abc", 3600));

        var authService = new AuthService(dbContext, passwordHasherMock, jwtGeneratorMock);

        // Act
        var result = await authService.RefreshTokenAsync(rawToken, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.RefreshToken.Should().Be("new_raw_refresh_token_456");
        result.AccessToken.Should().Be("access_token_abc");

        var dbOldToken = await dbContext.RefreshTokens.FirstAsync(rt => rt.Id == oldToken.Id);
        dbOldToken.RevokedAt.Should().NotBeNull();
        dbOldToken.RevokedReason.Should().Be(RevokeReason.Rotated);
        dbOldToken.ReplacedByTokenId.Should().NotBeNull();
        dbOldToken.ReplacedByTokenId.Should().NotBe(Guid.Empty);

        var dbNewToken = await dbContext.RefreshTokens.FirstAsync(rt => rt.TokenHash == "new_hashed_refresh_token_456");
        dbNewToken.Id.Should().NotBe(Guid.Empty);
        dbOldToken.ReplacedByTokenId.Should().Be(dbNewToken.Id);
        dbOldToken.ReplacedByToken.Should().Be(dbNewToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_Should_Throw_Unauthorized_When_Token_Expired()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var jwtGeneratorMock = Substitute.For<IJwtTokenGenerator>();
        var passwordHasherMock = Substitute.For<IPasswordHasher>();

        var user = new User { Id = Guid.NewGuid(), FullName = "Test User", Email = "expired@test.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);

        var rawToken = "expired_raw_token";
        var hashedToken = "expired_hashed_token";
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hashedToken,
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            IpAddress = IPAddress.Loopback,
            IssuedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        dbContext.RefreshTokens.Add(expiredToken);
        await dbContext.SaveChangesAsync();

        jwtGeneratorMock.HashRefreshToken(rawToken).Returns(hashedToken);
        var authService = new AuthService(dbContext, passwordHasherMock, jwtGeneratorMock);

        // Act & Assert
        Func<Task> act = async () => await authService.RefreshTokenAsync(rawToken, "127.0.0.1");
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Refresh token has expired.");
    }

    [Fact]
    public async Task RefreshTokenAsync_Should_Revoke_Entire_Family_On_Revoked_Token_Reuse()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var jwtGeneratorMock = Substitute.For<IJwtTokenGenerator>();
        var passwordHasherMock = Substitute.For<IPasswordHasher>();

        var user = new User { Id = Guid.NewGuid(), FullName = "Test User", Email = "reuse@test.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);

        var familyId = Guid.NewGuid();
        var rawToken = "reused_raw_token";
        var hashedToken = "reused_hashed_token";

        var oldRevokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hashedToken,
            FamilyId = familyId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            RevokedReason = RevokeReason.Rotated,
            IpAddress = IPAddress.Loopback,
            IssuedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var activeSiblingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "active_sibling_hash",
            FamilyId = familyId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IpAddress = IPAddress.Loopback,
            IssuedAt = DateTimeOffset.UtcNow
        };

        dbContext.RefreshTokens.AddRange(oldRevokedToken, activeSiblingToken);
        await dbContext.SaveChangesAsync();

        jwtGeneratorMock.HashRefreshToken(rawToken).Returns(hashedToken);
        var authService = new AuthService(dbContext, passwordHasherMock, jwtGeneratorMock);

        // Act & Assert
        Func<Task> act = async () => await authService.RefreshTokenAsync(rawToken, "127.0.0.1");
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Session revoked due to security violation. Please log in again.");

        var updatedSibling = await dbContext.RefreshTokens.FirstAsync(rt => rt.Id == activeSiblingToken.Id);
        updatedSibling.RevokedAt.Should().NotBeNull();
        updatedSibling.RevokedReason.Should().Be(RevokeReason.TheftDetected);
    }
}
