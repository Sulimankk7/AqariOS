using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Configuration;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.DTOs.Identity.Validators;
using PropertyOS.Application.Identity;
using PropertyOS.Domain.Identity.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Identity;

public class AuthServiceUnitTests
{
    private readonly IAuthService _authServiceMock;
    private readonly IPasswordHasher _passwordHasherMock;
    private readonly IJwtTokenGenerator _jwtTokenGeneratorMock;

    public AuthServiceUnitTests()
    {
        _authServiceMock = Substitute.For<IAuthService>();
        _passwordHasherMock = Substitute.For<IPasswordHasher>();
        _jwtTokenGeneratorMock = Substitute.For<IJwtTokenGenerator>();
    }

    [Fact]
    public void LoginRequestDtoValidator_Should_Pass_For_Valid_Data()
    {
        // Arrange
        var validator = new LoginRequestDtoValidator();
        var dto = new LoginRequestDto
        {
            EmailOrPhone = "user@propertyos.com",
            Password = "SecurePassword123!"
        };

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginRequestDtoValidator_Should_Fail_When_EmailOrPhone_Is_Empty()
    {
        // Arrange
        var validator = new LoginRequestDtoValidator();
        var dto = new LoginRequestDto
        {
            EmailOrPhone = "",
            Password = "SecurePassword123!"
        };

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "EmailOrPhone");
    }

    [Fact]
    public void OtpVerifyDtoValidator_Should_Fail_For_Invalid_Code_Length()
    {
        // Arrange
        var validator = new OtpVerifyDtoValidator();
        var dto = new OtpVerifyDto
        {
            Phone = "+962791234567",
            Code = "123", // Needs 6 digits
            Purpose = OtpPurpose.Login
        };

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Code");
    }

    [Fact]
    public void PasswordHasher_Should_Create_Argon2id_Hashes()
    {
        // Arrange
        var hasher = new PropertyOS.Infrastructure.Identity.PasswordHasher();
        var password = "Argon2idPassword123!";

        // Act
        var hash = hasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("argon2id.");
    }

    [Fact]
    public void PasswordHasher_Should_Verify_Correct_Password_With_Argon2id()
    {
        // Arrange
        var hasher = new PropertyOS.Infrastructure.Identity.PasswordHasher();
        var password = "CorrectPassword123!";
        var hash = hasher.HashPassword(password);

        // Act
        var isValid = hasher.VerifyPassword(password, hash);
        var isInvalid = hasher.VerifyPassword("WrongPassword123!", hash);

        // Assert
        isValid.Should().BeTrue();
        isInvalid.Should().BeFalse();
    }

    [Fact]
    public void PasswordHasher_Should_Reject_Malformed_Hashes_Without_Throwing()
    {
        // Arrange
        var hasher = new PropertyOS.Infrastructure.Identity.PasswordHasher();
        var password = "SomePassword123!";

        // Act & Assert
        hasher.VerifyPassword(password, "pbkdf2.1000.salt.hash").Should().BeFalse();
        hasher.VerifyPassword(password, "argon2id.invalid_salt_and_hash").Should().BeFalse();
        hasher.VerifyPassword(password, "invalid_format").Should().BeFalse();
        hasher.VerifyPassword(password, "").Should().BeFalse();
        hasher.VerifyPassword(password, "argon2id.4.65536.2.not_base64_salt.not_base64_hash").Should().BeFalse();
    }

    [Fact]
    public void PasswordHasher_Should_Generate_Unique_Salts_For_Same_Password()
    {
        // Arrange
        var hasher = new PropertyOS.Infrastructure.Identity.PasswordHasher();
        var password = "SamePassword123!";

        // Act
        var hash1 = hasher.HashPassword(password);
        var hash2 = hasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2);
        hasher.VerifyPassword(password, hash1).Should().BeTrue();
        hasher.VerifyPassword(password, hash2).Should().BeTrue();
    }

    [Fact]
    public void JwtTokenGenerator_Should_Generate_Token_And_Hash_Refresh_Token()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
            {
                { "Jwt:Secret", "SuperSecretKeyForPropertyOSWith32CharsLength!" },
                { "Jwt:Issuer", "PropertyOS" },
                { "Jwt:Audience", "PropertyOS-Clients" },
                { "Jwt:ExpiryMinutes", "15" }
            })
            .Build();

        var generator = new PropertyOS.Infrastructure.Identity.JwtTokenGenerator(config);
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        // Act
        var (token, expiresIn) = generator.GenerateAccessToken(userId, companyId, new[] { "Admin" }, new[] { "documents.view_confidential" });
        var rawRefreshToken = generator.GenerateRefreshToken();
        var hashedRefreshToken = generator.HashRefreshToken(rawRefreshToken);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        expiresIn.Should().Be(900);
        rawRefreshToken.Should().NotBeNullOrWhiteSpace();
        hashedRefreshToken.Should().NotBeNullOrWhiteSpace();
        hashedRefreshToken.Should().NotBe(rawRefreshToken);
    }
}
