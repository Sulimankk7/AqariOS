using System.Net;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using PropertyOS.Api.Controllers;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Infrastructure.Identity;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Tests.Unit.Infrastructure.Identity;

public sealed class RememberMeSessionTests
{
    private const string Password = "RememberMePassword123!";

    [Fact]
    public void LoginRequestDto_DeserializesRememberMe()
    {
        var dto = JsonSerializer.Deserialize<LoginRequestDto>(
            """{"EmailOrPhone":"user@example.test","Password":"secret","RememberMe":true}""");

        dto.Should().NotBeNull();
        dto!.RememberMe.Should().BeTrue();
    }

    [Fact]
    public async Task Login_OmittedRememberMe_DefaultsPublicRequestToFalse()
    {
        var authService = Substitute.For<IAuthService>();
        var controller = CreateController(new LoginResponseDto
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            User = new UserProfileDto()
        }, authService);
        var request = new LoginRequestDto
        {
            EmailOrPhone = "user@example.test",
            Password = "secret"
        };

        await controller.Login(request);

        await authService.Received(1).LoginAsync(
            Arg.Is<LoginRequestDto>(dto => dto.RememberMe == false),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_RememberMeFalse_CreatesFiniteNonPersistentSession()
    {
        await using var db = CreateDbContext();
        var service = CreateAuthService(db, normalLifetimeDays: 5);
        var before = DateTimeOffset.UtcNow;

        var result = await service.LoginAsync(CreateLoginRequest(rememberMe: false), "127.0.0.1", "unit-test");

        var token = await db.RefreshTokens.SingleAsync();
        token.IsPersistent.Should().BeFalse();
        token.AbsoluteSessionExpiresAt.Should().BeNull();
        token.ExpiresAt.Should().BeCloseTo(before.AddDays(5), TimeSpan.FromSeconds(2));
        result.IsPersistentSession.Should().BeFalse();
        result.RefreshTokenExpiresAt.Should().Be(token.ExpiresAt);
    }

    [Fact]
    public async Task LoginAsync_RememberMeTrue_UsesExactConfiguredThirtyDayAbsoluteExpiration()
    {
        await using var db = CreateDbContext();
        var service = CreateAuthService(db, rememberedLifetimeDays: 30);
        var before = DateTimeOffset.UtcNow;

        var result = await service.LoginAsync(CreateLoginRequest(rememberMe: true), "127.0.0.1", "unit-test");

        var token = await db.RefreshTokens.SingleAsync();
        token.IsPersistent.Should().BeTrue();
        token.ExpiresAt.Should().BeCloseTo(before.AddDays(30), TimeSpan.FromSeconds(2));
        token.AbsoluteSessionExpiresAt.Should().Be(token.ExpiresAt);
        result.IsPersistentSession.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefreshTokenAsync_PreservesSessionPersistenceMode(bool isPersistent)
    {
        await using var db = CreateDbContext();
        var service = CreateAuthService(db);
        var rawToken = "original-refresh-token";
        var originalExpiry = DateTimeOffset.UtcNow.AddDays(isPersistent ? 30 : 5);
        var absoluteExpiry = isPersistent ? originalExpiry : (DateTimeOffset?)null;
        await SeedRefreshTokenAsync(db, rawToken, isPersistent, originalExpiry, absoluteExpiry);

        var result = await service.RefreshTokenAsync(rawToken, "127.0.0.1");

        var replacement = await db.RefreshTokens.SingleAsync(token => token.RevokedAt == null);
        replacement.IsPersistent.Should().Be(isPersistent);
        replacement.AbsoluteSessionExpiresAt.Should().Be(absoluteExpiry);
        result.IsPersistentSession.Should().Be(isPersistent);
    }

    [Fact]
    public async Task RefreshTokenAsync_RememberedSession_PreservesOriginalAbsoluteExpiration()
    {
        await using var db = CreateDbContext();
        var service = CreateAuthService(db);
        var rawToken = "remembered-refresh-token";
        var absoluteExpiry = DateTimeOffset.UtcNow.AddDays(12);
        await SeedRefreshTokenAsync(db, rawToken, true, absoluteExpiry, absoluteExpiry);

        await service.RefreshTokenAsync(rawToken, "127.0.0.1");

        var replacement = await db.RefreshTokens.SingleAsync(token => token.RevokedAt == null);
        replacement.ExpiresAt.Should().Be(absoluteExpiry);
        replacement.AbsoluteSessionExpiresAt.Should().Be(absoluteExpiry);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredRememberedSession_IsRejected()
    {
        await using var db = CreateDbContext();
        var service = CreateAuthService(db);
        var rawToken = "expired-remembered-refresh-token";
        var expiredAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await SeedRefreshTokenAsync(db, rawToken, true, expiredAt, expiredAt);

        var action = () => service.RefreshTokenAsync(rawToken, "127.0.0.1");

        await action.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token has expired.");
    }

    [Fact]
    public async Task Login_RememberMeFalse_IssuesSessionCookieWithoutExpiresOrMaxAge()
    {
        var controller = CreateController(new LoginResponseDto
        {
            AccessToken = "access",
            RefreshToken = "refresh-secret",
            IsPersistentSession = false,
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            User = new UserProfileDto()
        });

        await controller.Login(CreateLoginRequest(false));

        var setCookie = controller.Response.Headers["Set-Cookie"].ToString();
        var normalizedCookie = setCookie.ToLowerInvariant();
        setCookie.Should().Contain("refreshToken=refresh-secret");
        normalizedCookie.Should().Contain("path=/");
        normalizedCookie.Should().Contain("secure");
        normalizedCookie.Should().Contain("httponly");
        normalizedCookie.Should().Contain("samesite=strict");
        normalizedCookie.Should().NotContain("expires=");
        normalizedCookie.Should().NotContain("max-age=");
    }

    [Fact]
    public async Task Login_RememberMeTrue_IssuesPersistentCookieAtAbsoluteExpiration()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var controller = CreateController(new LoginResponseDto
        {
            AccessToken = "access",
            RefreshToken = "refresh-secret",
            IsPersistentSession = true,
            RefreshTokenExpiresAt = expiresAt,
            User = new UserProfileDto()
        });

        await controller.Login(CreateLoginRequest(true));

        var setCookie = controller.Response.Headers["Set-Cookie"].ToString();
        setCookie.Should().Contain("refreshToken=refresh-secret");
        setCookie.ToLowerInvariant().Should().Contain("expires=");
        setCookie.Should().Contain(expiresAt.ToString("R"));
    }

    [Fact]
    public void LoginResponse_DoesNotSerializeRefreshToken()
    {
        var response = new LoginResponseDto
        {
            AccessToken = "access",
            RefreshToken = "must-not-be-visible",
            User = new UserProfileDto()
        };

        JsonSerializer.Serialize(response).Should().NotContain("must-not-be-visible");
    }

    [Fact]
    public async Task Logout_RevokesCurrentServerSessionAndDeletesMatchingCookiePath()
    {
        var authService = Substitute.For<IAuthService>();
        var controller = CreateController(new LoginResponseDto(), authService);
        controller.Request.Headers.Cookie = "refreshToken=current-refresh-token";

        var result = await controller.Logout(null);

        result.Should().BeOfType<NoContentResult>();
        await authService.Received(1).RevokeTokenAsync("current-refresh-token", Arg.Any<CancellationToken>());
        controller.Response.Headers["Set-Cookie"].ToString().ToLowerInvariant().Should().Contain("path=/");
    }

    [Fact]
    public async Task LogoutAll_RevokesRememberedSessionsForAuthenticatedUser()
    {
        var userId = Guid.NewGuid();
        var authService = Substitute.For<IAuthService>();
        var controller = CreateController(new LoginResponseDto(), authService);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test"));

        var result = await controller.LogoutAll();

        result.Should().BeOfType<NoContentResult>();
        await authService.Received(1).LogoutAllSessionsAsync(userId, Arg.Any<CancellationToken>());
    }

    private static LoginRequestDto CreateLoginRequest(bool rememberMe) => new()
    {
        EmailOrPhone = "remember@example.test",
        Password = Password,
        RememberMe = rememberMe
    };

    private static PropertyOsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    private static AuthService CreateAuthService(
        PropertyOsDbContext db,
        int normalLifetimeDays = 7,
        int rememberedLifetimeDays = 30)
    {
        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.VerifyPassword(Password, "hash").Returns(true);

        var jwtGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtGenerator.GenerateRefreshToken().Returns(_ => $"refresh-{Guid.NewGuid():N}");
        jwtGenerator.HashRefreshToken(Arg.Any<string>()).Returns(call => $"hash-{call.Arg<string>()}");
        jwtGenerator.GenerateAccessToken(
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IEnumerable<string>>())
            .Returns(("access-token", 3600));

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "remember@example.test",
            FullName = "Remember User",
            PasswordHash = "hash",
            IsActive = true
        });
        db.SaveChanges();

        return new AuthService(
            db,
            passwordHasher,
            jwtGenerator,
            refreshSessionOptions: Options.Create(new RefreshSessionOptions
            {
                NormalLifetimeDays = normalLifetimeDays,
                RememberedLifetimeDays = rememberedLifetimeDays
            }));
    }

    private static async Task SeedRefreshTokenAsync(
        PropertyOsDbContext db,
        string rawToken,
        bool isPersistent,
        DateTimeOffset expiresAt,
        DateTimeOffset? absoluteExpiresAt)
    {
        var user = await db.Users.SingleAsync();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = $"hash-{rawToken}",
            FamilyId = Guid.NewGuid(),
            ExpiresAt = expiresAt,
            IsPersistent = isPersistent,
            AbsoluteSessionExpiresAt = absoluteExpiresAt,
            IpAddress = IPAddress.Loopback,
            IssuedAt = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
    }

    private static AuthController CreateController(
        LoginResponseDto loginResponse,
        IAuthService? authService = null)
    {
        authService ??= Substitute.For<IAuthService>();
        authService.LoginAsync(
                Arg.Any<LoginRequestDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(loginResponse);

        return new AuthController(authService, Substitute.For<ISender>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
