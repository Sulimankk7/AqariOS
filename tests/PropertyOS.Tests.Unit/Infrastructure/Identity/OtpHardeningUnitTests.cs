using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Infrastructure.Identity;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Infrastructure.Identity;

public class OtpHardeningUnitTests
{
    private static PropertyOsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    private static async Task AddEligibleUserAsync(PropertyOsDbContext dbContext, string phone = "+962791234567")
    {
        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "OTP User",
            Phone = phone,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task RequestOtpAsync_Should_Send_SixDigit_Code_Through_Abstraction()
    {
        using var dbContext = CreateDbContext();
        await AddEligibleUserAsync(dbContext);
        var smsSender = Substitute.For<ISmsSender>();
        smsSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        var authService = new AuthService(dbContext, Substitute.For<IPasswordHasher>(), Substitute.For<IJwtTokenGenerator>(),
            smsSender: smsSender,
            otpOptions: Options.Create(new OtpOptions { HashKey = "test-hmac-key", ResendCooldownSeconds = 0 }));

        var code = await authService.RequestOtpAsync(new OtpRequestDto { Phone = "+962791234567", Purpose = OtpPurpose.Login });

        code.Should().MatchRegex("^\\d{6}$");
        await smsSender.Received(1).SendAsync("+962791234567", Arg.Is<string>(message => message.Contains(code)), Arg.Any<CancellationToken>());
        (await dbContext.OtpChallenges.SingleAsync()).CodeHash.Should().NotBe(code);
    }

    [Fact]
    public async Task RequestOtpAsync_UnregisteredPhone_DoesNotSendOrCreateChallenge()
    {
        using var dbContext = CreateDbContext();
        var smsSender = Substitute.For<ISmsSender>();
        var authService = new AuthService(
            dbContext,
            Substitute.For<IPasswordHasher>(),
            Substitute.For<IJwtTokenGenerator>(),
            smsSender: smsSender,
            otpOptions: Options.Create(new OtpOptions { HashKey = "test-hmac-key" }));

        Func<Task> act = () => authService.RequestOtpAsync(
            new OtpRequestDto { Phone = "+962789425056", Purpose = OtpPurpose.Login });

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("No account is registered with this phone number.");
        await smsSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        (await dbContext.OtpChallenges.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RequestOtpAsync_SoftDeletedUser_DoesNotSendOrCreateChallenge()
    {
        using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Deleted OTP User",
            Phone = "+962791234567",
            IsActive = true,
            DeletedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var smsSender = Substitute.For<ISmsSender>();
        var authService = new AuthService(
            dbContext,
            Substitute.For<IPasswordHasher>(),
            Substitute.For<IJwtTokenGenerator>(),
            smsSender: smsSender,
            otpOptions: Options.Create(new OtpOptions { HashKey = "test-hmac-key" }));

        Func<Task> act = () => authService.RequestOtpAsync(
            new OtpRequestDto { Phone = "+962791234567", Purpose = OtpPurpose.Login });

        await act.Should().ThrowAsync<NotFoundException>();
        await smsSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        (await dbContext.OtpChallenges.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("0791234567")]
    [InlineData("079 123 4567")]
    [InlineData("079-123-4567")]
    [InlineData("+962791234567")]
    [InlineData("+962 79 123 4567")]
    public async Task RequestOtpAsync_EquivalentFormattedPhone_ResolvesRegisteredUser(string input)
    {
        using var dbContext = CreateDbContext();
        await AddEligibleUserAsync(dbContext);
        var smsSender = Substitute.For<ISmsSender>();
        smsSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        var authService = new AuthService(
            dbContext,
            Substitute.For<IPasswordHasher>(),
            Substitute.For<IJwtTokenGenerator>(),
            smsSender: smsSender,
            otpOptions: Options.Create(new OtpOptions { HashKey = "test-hmac-key" }));

        await authService.RequestOtpAsync(
            new OtpRequestDto { Phone = input, Purpose = OtpPurpose.Login });

        await smsSender.Received(1).SendAsync(
            "+962791234567", Arg.Any<string>(), Arg.Any<CancellationToken>());
        (await dbContext.OtpChallenges.SingleAsync()).Phone.Should().Be("+962791234567");
    }

    [Fact]
    public async Task RequestOtpAsync_RegisteredUser_PreservesDestinationCooldown()
    {
        using var dbContext = CreateDbContext();
        await AddEligibleUserAsync(dbContext);
        var smsSender = Substitute.For<ISmsSender>();
        smsSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        var authService = new AuthService(
            dbContext,
            Substitute.For<IPasswordHasher>(),
            Substitute.For<IJwtTokenGenerator>(),
            smsSender: smsSender,
            otpOptions: Options.Create(new OtpOptions { HashKey = "test-hmac-key", ResendCooldownSeconds = 60 }));
        var request = new OtpRequestDto { Phone = "+962791234567", Purpose = OtpPurpose.Login };

        await authService.RequestOtpAsync(request);
        Func<Task> secondRequest = () => authService.RequestOtpAsync(request);

        await secondRequest.Should().ThrowAsync<OtpRequestThrottledException>();
        await smsSender.Received(1).SendAsync(
            "+962791234567", Arg.Any<string>(), Arg.Any<CancellationToken>());
        (await dbContext.OtpChallenges.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RequestOtpAsync_Should_Invalidate_Previous_Challenge_When_Replaced()
    {
        using var dbContext = CreateDbContext();
        await AddEligibleUserAsync(dbContext);
        var smsSender = Substitute.For<ISmsSender>();
        smsSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var authService = new AuthService(dbContext, Substitute.For<IPasswordHasher>(), Substitute.For<IJwtTokenGenerator>(),
            smsSender: smsSender,
            otpOptions: Options.Create(new OtpOptions { HashKey = "test-hmac-key", ResendCooldownSeconds = 0 }));
        var request = new OtpRequestDto { Phone = "+962791234567", Purpose = OtpPurpose.Login };

        await authService.RequestOtpAsync(request);
        await authService.RequestOtpAsync(request);

        var challenges = await dbContext.OtpChallenges.OrderBy(c => c.CreatedAt).ToListAsync();
        challenges.Should().HaveCount(2);
        challenges[0].ConsumedAt.Should().NotBeNull();
        challenges[1].ConsumedAt.Should().BeNull();
    }

    [Fact]
    public async Task RequestOtpAsync_Should_Create_Challenge_With_Client_IP()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        await AddEligibleUserAsync(dbContext);
        var jwtGeneratorMock = Substitute.For<IJwtTokenGenerator>();
        var passwordHasherMock = Substitute.For<IPasswordHasher>();
        var authService = new AuthService(dbContext, passwordHasherMock, jwtGeneratorMock);

        var dto = new OtpRequestDto
        {
            Phone = "+962791234567",
            Purpose = OtpPurpose.Login
        };

        // Act
        var code = await authService.RequestOtpAsync(dto, "192.168.1.50");

        // Assert
        code.Should().HaveLength(6);
        var challenge = await dbContext.OtpChallenges.FirstOrDefaultAsync(c => c.Phone == "+962791234567");
        challenge.Should().NotBeNull();
        challenge!.RequestedIp.Should().Be(IPAddress.Parse("192.168.1.50"));
        challenge.MaxAttempts.Should().Be(3);
        challenge.FailedAttempts.Should().Be(0);
        challenge.ConsumedAt.Should().BeNull();
    }

    [Fact]
    public async Task VerifyOtpAsync_Should_Increment_FailedAttempts_On_Wrong_Code()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        await AddEligibleUserAsync(dbContext);
        var jwtGeneratorMock = Substitute.For<IJwtTokenGenerator>();
        var passwordHasherMock = Substitute.For<IPasswordHasher>();
        var authService = new AuthService(dbContext, passwordHasherMock, jwtGeneratorMock);

        var dto = new OtpRequestDto { Phone = "+962791234567", Purpose = OtpPurpose.Login };
        await authService.RequestOtpAsync(dto, "127.0.0.1");

        var verifyDto = new OtpVerifyDto
        {
            Phone = "+962791234567",
            Code = "000000", // Incorrect code
            Purpose = OtpPurpose.Login
        };

        // Act & Assert
        Func<Task> act = async () => await authService.VerifyOtpAsync(verifyDto, "127.0.0.1", "Agent");
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("OTP code is invalid.");

        var challenge = await dbContext.OtpChallenges.FirstAsync(c => c.Phone == "+962791234567");
        challenge.FailedAttempts.Should().Be(1);
    }

    [Fact]
    public async Task VerifyOtpAsync_Should_Lockout_And_Reject_After_MaxAttempts_Exceeded()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        await AddEligibleUserAsync(dbContext);
        var jwtGeneratorMock = Substitute.For<IJwtTokenGenerator>();
        var passwordHasherMock = Substitute.For<IPasswordHasher>();
        var authService = new AuthService(dbContext, passwordHasherMock, jwtGeneratorMock);

        var dto = new OtpRequestDto { Phone = "+962791234567", Purpose = OtpPurpose.Login };
        var realCode = await authService.RequestOtpAsync(dto, "127.0.0.1");

        var wrongVerifyDto = new OtpVerifyDto { Phone = "+962791234567", Code = "000000", Purpose = OtpPurpose.Login };

        // Attempt 1 wrong
        try { await authService.VerifyOtpAsync(wrongVerifyDto, "127.0.0.1", "Agent"); } catch { }
        // Attempt 2 wrong
        try { await authService.VerifyOtpAsync(wrongVerifyDto, "127.0.0.1", "Agent"); } catch { }
        // Attempt 3 wrong
        try { await authService.VerifyOtpAsync(wrongVerifyDto, "127.0.0.1", "Agent"); } catch { }

        var challengeAfter3 = await dbContext.OtpChallenges.FirstAsync(c => c.Phone == "+962791234567");
        challengeAfter3.FailedAttempts.Should().Be(3);

        // Attempt 4 even with CORRECT code must FAIL
        var correctVerifyDto = new OtpVerifyDto { Phone = "+962791234567", Code = realCode, Purpose = OtpPurpose.Login };
        Func<Task> actAttempt4 = async () => await authService.VerifyOtpAsync(correctVerifyDto, "127.0.0.1", "Agent");

        await actAttempt4.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("OTP verification attempts exceeded. Please request a new OTP.");
    }

    [Fact]
    public async Task VerifyOtpAsync_Should_Succeed_With_Correct_Code_And_Not_Increment_FailedAttempts()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var jwtGeneratorMock = Substitute.For<IJwtTokenGenerator>();
        var passwordHasherMock = Substitute.For<IPasswordHasher>();

        var user = new User { Id = Guid.NewGuid(), FullName = "OTP User", Phone = "+962791234567", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        jwtGeneratorMock.GenerateAccessToken(user.Id, Arg.Any<Guid?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
            .Returns(("access_token_123", 3600));
        jwtGeneratorMock.GenerateRefreshToken().Returns("refresh_token_123");
        jwtGeneratorMock.HashRefreshToken("refresh_token_123").Returns("hash_123");

        var authService = new AuthService(dbContext, passwordHasherMock, jwtGeneratorMock);

        var dto = new OtpRequestDto { Phone = "+962791234567", Purpose = OtpPurpose.Login };
        var realCode = await authService.RequestOtpAsync(dto, "127.0.0.1");

        var correctVerifyDto = new OtpVerifyDto { Phone = "+962791234567", Code = realCode, Purpose = OtpPurpose.Login };

        // Act
        var result = await authService.VerifyOtpAsync(correctVerifyDto, "127.0.0.1", "Agent");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token_123");

        var challenge = await dbContext.OtpChallenges.FirstAsync(c => c.Phone == "+962791234567");
        challenge.ConsumedAt.Should().NotBeNull();
        challenge.FailedAttempts.Should().Be(0);
    }

    [Fact]
    public async Task RevokeTokenAsync_Should_Revoke_Specific_Active_Token()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var jwtGeneratorMock = Substitute.For<IJwtTokenGenerator>();
        var passwordHasherMock = Substitute.For<IPasswordHasher>();

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "token_hash_abc",
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            IpAddress = IPAddress.Loopback,
            IssuedAt = DateTimeOffset.UtcNow
        };
        dbContext.RefreshTokens.Add(token);
        await dbContext.SaveChangesAsync();

        jwtGeneratorMock.HashRefreshToken("raw_abc").Returns("token_hash_abc");
        var authService = new AuthService(dbContext, passwordHasherMock, jwtGeneratorMock);

        // Act
        await authService.RevokeTokenAsync("raw_abc");

        // Assert
        var revokedToken = await dbContext.RefreshTokens.FirstAsync(rt => rt.Id == token.Id);
        revokedToken.RevokedAt.Should().NotBeNull();
        revokedToken.RevokedReason.Should().Be(RevokeReason.Logout);
    }

    [Fact]
    public async Task LogoutAllSessionsAsync_Should_Revoke_All_User_Tokens()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var jwtGeneratorMock = Substitute.For<IJwtTokenGenerator>();
        var passwordHasherMock = Substitute.For<IPasswordHasher>();
        var userId = Guid.NewGuid();

        var token1 = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "h1", FamilyId = Guid.NewGuid(), ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), IpAddress = IPAddress.Loopback };
        var token2 = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = "h2", FamilyId = Guid.NewGuid(), ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), IpAddress = IPAddress.Loopback };
        dbContext.RefreshTokens.AddRange(token1, token2);
        await dbContext.SaveChangesAsync();

        var authService = new AuthService(dbContext, passwordHasherMock, jwtGeneratorMock);

        // Act
        await authService.LogoutAllSessionsAsync(userId);

        // Assert
        var tokens = await dbContext.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
        tokens.Should().AllSatisfy(t =>
        {
            t.RevokedAt.Should().NotBeNull();
            t.RevokedReason.Should().Be(RevokeReason.AdminRevoked);
        });
    }
}
