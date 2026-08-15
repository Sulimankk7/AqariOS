using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Identity.Queries.ValidateTenantActivationToken;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Identity;

public class ValidateTenantActivationTokenQueryHandlerTests
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
    public async Task Handle_ValidToken_ReturnsValidStatusWithTenantNameAndExpiry()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var rawToken = "valid-token-123";
        var hashedToken = HashToken(rawToken);
        var expiry = DateTimeOffset.UtcNow.AddHours(48);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Ahmad Al-Saleh",
            Phone = "+962791112223",
            PasswordResetTokenHash = hashedToken,
            PasswordResetExpiresAt = expiry,
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = new ValidateTenantActivationTokenQueryHandler(dbContext);
        var query = new ValidateTenantActivationTokenQuery(rawToken);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be("VALID");
        result.TenantName.Should().Be("Ahmad Al-Saleh");
        result.ExpiresAt.Should().Be(expiry);
        result.Message.Should().Contain("صالح");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsExpiredStatus()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var rawToken = "expired-token-123";
        var hashedToken = HashToken(rawToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Ahmad Al-Saleh",
            PasswordResetTokenHash = hashedToken,
            PasswordResetExpiresAt = DateTimeOffset.UtcNow.AddHours(-2), // Expired
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = new ValidateTenantActivationTokenQueryHandler(dbContext);
        var query = new ValidateTenantActivationTokenQuery(rawToken);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be("EXPIRED");
        result.Message.Should().Contain("انتهت صلاحية");
    }

    [Fact]
    public async Task Handle_NullOrEmptyToken_ReturnsInvalidStatus()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var handler = new ValidateTenantActivationTokenQueryHandler(dbContext);

        // Act
        var resultNull = await handler.Handle(new ValidateTenantActivationTokenQuery(null), CancellationToken.None);
        var resultEmpty = await handler.Handle(new ValidateTenantActivationTokenQuery("   "), CancellationToken.None);

        // Assert
        resultNull.Status.Should().Be("INVALID");
        resultEmpty.Status.Should().Be("INVALID");
    }

    [Fact]
    public async Task Handle_AlreadyConsumedOrNonExistentToken_ReturnsAlreadyUsedStatus()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var handler = new ValidateTenantActivationTokenQueryHandler(dbContext);
        var query = new ValidateTenantActivationTokenQuery("consumed-or-random-token");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be("ALREADY_USED");
        result.Message.Should().Contain("غير صالح أو تم استخدامه");
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsInvalidStatus()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var rawToken = "deactivated-token-123";
        var hashedToken = HashToken(rawToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Deactivated User",
            PasswordResetTokenHash = hashedToken,
            PasswordResetExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            IsActive = false // Deactivated
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = new ValidateTenantActivationTokenQueryHandler(dbContext);
        var query = new ValidateTenantActivationTokenQuery(rawToken);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be("INVALID");
        result.Message.Should().Contain("معطل");
    }
}
