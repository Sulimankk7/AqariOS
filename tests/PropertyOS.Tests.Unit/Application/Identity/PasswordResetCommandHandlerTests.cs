using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Options;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity;
using PropertyOS.Application.Identity.Commands.PasswordReset;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Infrastructure.Identity;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Tests.Unit.Application.Identity;

public sealed class PasswordResetCommandHandlerTests
{
    private const string HashKey = "password-reset-test-hmac-key-at-least-32-bytes";

    private static PropertyOsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    private static User AddUser(
        PropertyOsDbContext dbContext,
        string? email = "reset@example.com",
        string? phone = "+962791234567",
        bool active = true,
        bool deleted = false)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Reset User",
            Email = email,
            Phone = phone,
            IsActive = active,
            DeletedAt = deleted ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(user);
        dbContext.SaveChanges();
        return user;
    }

    private static RequestPasswordResetCommandHandler CreateRequestHandler(
        PropertyOsDbContext dbContext,
        IEmailSender emailSender,
        ISmsSender smsSender,
        PasswordResetOptions? options = null)
    {
        options ??= new PasswordResetOptions();
        options.MinimumRequestDurationMilliseconds = 0;
        return new(
            dbContext,
            emailSender,
            smsSender,
            Options.Create(options ?? new PasswordResetOptions()),
            Options.Create(new OtpOptions { HashKey = HashKey }),
            Options.Create(new FrontendOptions { BaseUrl = "https://app.example.test" }),
            NullLogger<RequestPasswordResetCommandHandler>.Instance);
    }

    [Fact]
    public async Task RegisteredEmail_PersistsOnlyHash_AndSendsResetUrl()
    {
        using var dbContext = CreateDbContext();
        AddUser(dbContext);
        var emailSender = Substitute.For<IEmailSender>();
        var smsSender = Substitute.For<ISmsSender>();
        string? html = null;
        emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                html = call.ArgAt<string>(2);
                return Task.FromResult(true);
            });
        var handler = CreateRequestHandler(dbContext, emailSender, smsSender);

        var response = await handler.Handle(
            new RequestPasswordResetCommand(PasswordResetDeliveryMethod.Email, "RESET@EXAMPLE.COM", "127.0.0.1", "test"),
            CancellationToken.None);

        response.Message.Should().Be("If an eligible account exists, recovery instructions have been sent.");
        var challenge = await dbContext.PasswordResetChallenges.SingleAsync();
        challenge.Kind.Should().Be(PasswordResetChallengeKind.EmailToken);
        challenge.CredentialHash.Should().MatchRegex("^[a-f0-9]{64}$");
        var rawToken = Regex.Match(html!, @"token=([^&""<]+)").Groups[1].Value;
        rawToken.Should().NotBeNullOrWhiteSpace();
        challenge.CredentialHash.Should().NotBe(rawToken);
        await emailSender.Received(1).SendAsync("reset@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await smsSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default);
    }

    [Fact]
    public async Task RegisteredEmail_ProviderFailure_ConsumesChallengeAndPreservesGenericResponse()
    {
        using var dbContext = CreateDbContext();
        AddUser(dbContext);
        var emailSender = Substitute.For<IEmailSender>();
        emailSender.SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        var handler = CreateRequestHandler(dbContext, emailSender, Substitute.For<ISmsSender>());

        var response = await handler.Handle(
            new RequestPasswordResetCommand(
                PasswordResetDeliveryMethod.Email,
                "reset@example.com",
                "127.0.0.1",
                "test"),
            CancellationToken.None);

        response.Message.Should().Be("If an eligible account exists, recovery instructions have been sent.");
        var challenge = await dbContext.PasswordResetChallenges.SingleAsync();
        challenge.ConsumedAt.Should().NotBeNull();
        await emailSender.Received(1).SendAsync(
            "reset@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task IneligibleOrUnknownEmail_ReturnsSameResponseWithoutDelivery(bool createUser, bool active, bool deleted)
    {
        using var dbContext = CreateDbContext();
        if (createUser) AddUser(dbContext, active: active, deleted: deleted);
        var emailSender = Substitute.For<IEmailSender>();
        var handler = CreateRequestHandler(dbContext, emailSender, Substitute.For<ISmsSender>());

        var response = await handler.Handle(
            new RequestPasswordResetCommand(PasswordResetDeliveryMethod.Email, "reset@example.com", null, null),
            CancellationToken.None);

        response.Message.Should().Be("If an eligible account exists, recovery instructions have been sent.");
        await emailSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default, default);
        (await dbContext.PasswordResetChallenges.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("0791234567")]
    [InlineData("079 123 4567")]
    [InlineData("079-123-4567")]
    [InlineData("+962791234567")]
    [InlineData("+962 79 123 4567")]
    public async Task RegisteredPhone_UsesCanonicalUserPhone_AndHashedOtp(string input)
    {
        using var dbContext = CreateDbContext();
        AddUser(dbContext);
        var smsSender = Substitute.For<ISmsSender>();
        string? message = null;
        smsSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                message = call.ArgAt<string>(1);
                return Task.FromResult(true);
            });
        var handler = CreateRequestHandler(dbContext, Substitute.For<IEmailSender>(), smsSender);

        await handler.Handle(
            new RequestPasswordResetCommand(PasswordResetDeliveryMethod.Phone, input, null, null),
            CancellationToken.None);

        var code = Regex.Match(message!, @"\d{6}").Value;
        code.Should().HaveLength(6);
        var challenge = await dbContext.PasswordResetChallenges.SingleAsync();
        challenge.Kind.Should().Be(PasswordResetChallengeKind.SmsOtp);
        challenge.CredentialHash.Should().NotBe(code);
        (await dbContext.OtpChallenges.CountAsync()).Should().Be(0);
        await smsSender.Received(1).SendAsync("+962791234567", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmailResetTokens_AreRandomAcrossRequests()
    {
        using var dbContext = CreateDbContext();
        AddUser(dbContext);
        var emailSender = Substitute.For<IEmailSender>();
        emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        var handler = CreateRequestHandler(dbContext, emailSender, Substitute.For<ISmsSender>(),
            new PasswordResetOptions { ResendCooldownSeconds = 0 });
        var command = new RequestPasswordResetCommand(PasswordResetDeliveryMethod.Email, "reset@example.com", null, null);

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        var hashes = await dbContext.PasswordResetChallenges.Select(challenge => challenge.CredentialHash).ToListAsync();
        hashes.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        await emailSender.Received(2).SendAsync(
            "reset@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownPhone_DoesNotPersistOrSend()
    {
        using var dbContext = CreateDbContext();
        var smsSender = Substitute.For<ISmsSender>();
        var handler = CreateRequestHandler(dbContext, Substitute.For<IEmailSender>(), smsSender);

        var response = await handler.Handle(
            new RequestPasswordResetCommand(PasswordResetDeliveryMethod.Phone, "+962799999999", null, null),
            CancellationToken.None);

        response.Message.Should().NotBeNullOrWhiteSpace();
        await smsSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default);
        (await dbContext.PasswordResetChallenges.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DestinationCooldown_ReturnsGenericResponseAndDoesNotSendAgain()
    {
        using var dbContext = CreateDbContext();
        AddUser(dbContext);
        var smsSender = Substitute.For<ISmsSender>();
        smsSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var handler = CreateRequestHandler(dbContext, Substitute.For<IEmailSender>(), smsSender,
            new PasswordResetOptions { ResendCooldownSeconds = 60 });
        var command = new RequestPasswordResetCommand(PasswordResetDeliveryMethod.Phone, "+962791234567", null, null);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        second.Message.Should().Be(first.Message);
        await smsSender.Received(1).SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        (await dbContext.PasswordResetChallenges.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DestinationRequestCap_ReturnsGenericResponseAndStopsAdditionalDelivery()
    {
        using var dbContext = CreateDbContext();
        AddUser(dbContext);
        var smsSender = Substitute.For<ISmsSender>();
        smsSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var handler = CreateRequestHandler(dbContext, Substitute.For<IEmailSender>(), smsSender,
            new PasswordResetOptions
            {
                ResendCooldownSeconds = 0,
                MaxRequestsPerDestinationWindow = 2,
                DestinationWindowMinutes = 60
            });
        var command = new RequestPasswordResetCommand(PasswordResetDeliveryMethod.Phone, "+962791234567", null, null);

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);
        var response = await handler.Handle(command, CancellationToken.None);

        response.Message.Should().Be("If an eligible account exists, recovery instructions have been sent.");
        await smsSender.Received(2).SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        (await dbContext.PasswordResetChallenges.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task NewSmsRequest_InvalidatesPreviousActivePasswordResetOtp()
    {
        using var dbContext = CreateDbContext();
        AddUser(dbContext);
        var smsSender = Substitute.For<ISmsSender>();
        smsSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var handler = CreateRequestHandler(dbContext, Substitute.For<IEmailSender>(), smsSender,
            new PasswordResetOptions { ResendCooldownSeconds = 0 });
        var command = new RequestPasswordResetCommand(PasswordResetDeliveryMethod.Phone, "+962791234567", null, null);

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        var challenges = await dbContext.PasswordResetChallenges.OrderBy(x => x.CreatedAt).ToListAsync();
        challenges.Should().HaveCount(2);
        challenges[0].ConsumedAt.Should().NotBeNull();
        challenges[1].ConsumedAt.Should().BeNull();
        challenges.Select(x => x.CredentialHash).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task SmsOtpVerification_ReturnsResetAuthorizationWithoutAuthenticationToken()
    {
        using var dbContext = CreateDbContext();
        var user = AddUser(dbContext);
        dbContext.PasswordResetChallenges.Add(new PasswordResetChallenge
        {
            Id = Guid.NewGuid(), UserId = user.Id, Kind = PasswordResetChallengeKind.SmsOtp,
            CredentialHash = HashOtp("123456"), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            MaxAttempts = 3, RequestedIp = System.Net.IPAddress.Loopback, CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var handler = new VerifyPasswordResetOtpCommandHandler(
            dbContext, Options.Create(new PasswordResetOptions()), Options.Create(new OtpOptions { HashKey = HashKey }));

        var result = await handler.Handle(
            new VerifyPasswordResetOtpCommand("+962791234567", "123456", "127.0.0.1"), CancellationToken.None);

        result.ResetAuthorization.Should().NotBeNullOrWhiteSpace();
        result.Should().NotBeAssignableTo<LoginResponseDto>();
        (await dbContext.PasswordResetChallenges.CountAsync(x => x.Kind == PasswordResetChallengeKind.SmsAuthorization)).Should().Be(1);
    }

    [Fact]
    public async Task SmsOtp_AttemptLimitAndExpiry_AreEnforced()
    {
        using var dbContext = CreateDbContext();
        var user = AddUser(dbContext);
        var challenge = new PasswordResetChallenge
        {
            Id = Guid.NewGuid(), UserId = user.Id, Kind = PasswordResetChallengeKind.SmsOtp,
            CredentialHash = HashOtp("123456"), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            FailedAttempts = 2, MaxAttempts = 3, RequestedIp = System.Net.IPAddress.Loopback,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.PasswordResetChallenges.Add(challenge);
        await dbContext.SaveChangesAsync();
        var handler = new VerifyPasswordResetOtpCommandHandler(
            dbContext, Options.Create(new PasswordResetOptions()), Options.Create(new OtpOptions { HashKey = HashKey }));

        Func<Task> wrong = () => handler.Handle(
            new VerifyPasswordResetOtpCommand("+962791234567", "000000", null), CancellationToken.None);
        await wrong.Should().ThrowAsync<BusinessRuleException>()
            .Where(exception => exception.Code == "PASSWORD_RESET_OTP_INVALID");

        Func<Task> exceeded = () => handler.Handle(
            new VerifyPasswordResetOtpCommand("+962791234567", "123456", null), CancellationToken.None);
        await exceeded.Should().ThrowAsync<BusinessRuleException>()
            .Where(exception => exception.Code == "PASSWORD_RESET_OTP_ATTEMPTS_EXCEEDED");

        challenge.FailedAttempts = 0;
        challenge.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        await dbContext.SaveChangesAsync();
        Func<Task> expired = () => handler.Handle(
            new VerifyPasswordResetOtpCommand("+962791234567", "123456", null), CancellationToken.None);
        await expired.Should().ThrowAsync<BusinessRuleException>()
            .Where(exception => exception.Code == "PASSWORD_RESET_OTP_EXPIRED");
    }

    [Fact]
    public async Task SuccessfulCompletion_HashesPassword_ConsumesToken_AndRevokesSessions()
    {
        using var dbContext = CreateDbContext();
        var user = AddUser(dbContext);
        const string rawToken = "email-reset-token-value";
        var challenge = new PasswordResetChallenge
        {
            Id = Guid.NewGuid(), UserId = user.Id, Kind = PasswordResetChallengeKind.EmailToken,
            CredentialHash = HashToken(rawToken), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(20),
            MaxAttempts = 1, RequestedIp = System.Net.IPAddress.Loopback, CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.PasswordResetChallenges.Add(challenge);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, TokenHash = "hash", FamilyId = Guid.NewGuid(),
            IpAddress = System.Net.IPAddress.Loopback, IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        await dbContext.SaveChangesAsync();
        var handler = new CompletePasswordResetCommandHandler(dbContext, new PasswordHasher());

        var response = await handler.Handle(
            new CompletePasswordResetCommand(rawToken, "ValidPassword1", "127.0.0.1", "test"), CancellationToken.None);

        response.Message.Should().Contain("successfully");
        user.PasswordHash.Should().StartWith("argon2id.");
        challenge.ConsumedAt.Should().NotBeNull();
        var refreshToken = await dbContext.RefreshTokens.SingleAsync();
        refreshToken.RevokedAt.Should().NotBeNull();
        refreshToken.RevokedReason.Should().Be(RevokeReason.PasswordReset);

        Func<Task> reuse = () => handler.Handle(
            new CompletePasswordResetCommand(rawToken, "AnotherPassword1", null, null), CancellationToken.None);
        await reuse.Should().ThrowAsync<BusinessRuleException>()
            .Where(exception => exception.Code == "PASSWORD_RESET_TOKEN_USED");
    }

    [Fact]
    public async Task ExpiredEmailToken_IsRejectedWithoutChangingPassword()
    {
        using var dbContext = CreateDbContext();
        var user = AddUser(dbContext);
        dbContext.PasswordResetChallenges.Add(new PasswordResetChallenge
        {
            Id = Guid.NewGuid(), UserId = user.Id, Kind = PasswordResetChallengeKind.EmailToken,
            CredentialHash = HashToken("expired"), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            MaxAttempts = 1, RequestedIp = System.Net.IPAddress.Loopback, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        });
        await dbContext.SaveChangesAsync();
        var handler = new CompletePasswordResetCommandHandler(dbContext, new PasswordHasher());

        Func<Task> act = () => handler.Handle(
            new CompletePasswordResetCommand("expired", "ValidPassword1", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(exception => exception.Code == "PASSWORD_RESET_TOKEN_EXPIRED");
        user.PasswordHash.Should().BeNull();
    }

    [Fact]
    public async Task InvalidResetToken_IsRejectedWithoutChangingPassword()
    {
        using var dbContext = CreateDbContext();
        var user = AddUser(dbContext);
        var handler = new CompletePasswordResetCommandHandler(dbContext, new PasswordHasher());

        Func<Task> act = () => handler.Handle(
            new CompletePasswordResetCommand("unknown-reset-token", "ValidPassword1", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(exception => exception.Code == "PASSWORD_RESET_TOKEN_INVALID");
        user.PasswordHash.Should().BeNull();
    }

    [Fact]
    public async Task LoginOtpChallenge_CannotCompletePasswordReset()
    {
        using var dbContext = CreateDbContext();
        AddUser(dbContext);
        dbContext.OtpChallenges.Add(new OtpChallenge
        {
            Id = Guid.NewGuid(), Phone = "+962791234567", Purpose = OtpPurpose.Login,
            CodeHash = HashOtp("123456"), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            MaxAttempts = 3, RequestedIp = System.Net.IPAddress.Loopback, CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var handler = new CompletePasswordResetCommandHandler(dbContext, new PasswordHasher());

        Func<Task> act = () => handler.Handle(
            new CompletePasswordResetCommand("123456", "ValidPassword1", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(exception => exception.Code == "PASSWORD_RESET_TOKEN_INVALID");
    }

    [Fact]
    public async Task ExpiredSmsAuthorization_IsRejectedAndCannotBeReused()
    {
        using var dbContext = CreateDbContext();
        var user = AddUser(dbContext);
        const string authorization = "sms-reset-authorization";
        dbContext.PasswordResetChallenges.Add(new PasswordResetChallenge
        {
            Id = Guid.NewGuid(), UserId = user.Id, Kind = PasswordResetChallengeKind.SmsAuthorization,
            CredentialHash = HashToken(authorization), ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1),
            MaxAttempts = 1, RequestedIp = System.Net.IPAddress.Loopback, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2)
        });
        await dbContext.SaveChangesAsync();
        var handler = new CompletePasswordResetCommandHandler(dbContext, new PasswordHasher());

        Func<Task> act = () => handler.Handle(
            new CompletePasswordResetCommand(authorization, "ValidPassword1", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(exception => exception.Code == "PASSWORD_RESET_AUTHORIZATION_EXPIRED");
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string HashOtp(string code) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(HashKey), Encoding.UTF8.GetBytes(code))).ToLowerInvariant();
}
