using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity.Commands.Register;
using PropertyOS.Application.Identity.Provisioning;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Identity;

public class RegisterCommandHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public void RegisterCommandValidator_Should_Pass_For_Valid_Data()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand(
            FullName: "Super Administrator",
            Email: "admin@propertyos.com",
            Phone: "+962791234567",
            Password: "SecurePassword123!",
            CompanyName: "AqariOS Property Management",
            DisplayName: "AqariOS",
            CompanyType: CompanyType.PropertyManagementCompany,
            CountryCode: "JO",
            PreferredLanguage: "ar"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterCommandValidator_Should_Fail_When_Both_Email_And_Phone_Are_Missing()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand(
            FullName: "John Doe",
            Email: null,
            Phone: null,
            Password: "SecurePassword123!",
            CompanyName: "Test Company"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Email or Phone"));
    }

    [Fact]
    public void RegisterCommandValidator_Should_Fail_When_Password_Is_Too_Short()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand(
            FullName: "John Doe",
            Email: "user@test.com",
            Phone: null,
            Password: "short",
            CompanyName: "Test Company"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Handle_Should_Throw_DuplicateEmailException_When_Email_Already_Exists()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var existingEmail = "existing@propertyos.com";
        dbContext.Users.Add(new User { Id = Guid.NewGuid(), Email = existingEmail, FullName = "Existing User" });
        await dbContext.SaveChangesAsync();

        var provisioningServiceMock = Substitute.For<ITenantProvisioningService>();
        var handler = new RegisterCommandHandler(dbContext, provisioningServiceMock);

        var command = new RegisterCommand(
            FullName: "New User",
            Email: existingEmail,
            Phone: null,
            Password: "SecurePassword123!",
            CompanyName: "New Company"
        );

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicateEmailException>()
            .WithMessage($"*{existingEmail}*");
    }

    [Fact]
    public async Task Handle_Should_Throw_DuplicatePhoneException_When_Phone_Already_Exists()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var existingPhone = "+962791234567";
        dbContext.Users.Add(new User { Id = Guid.NewGuid(), Phone = existingPhone, FullName = "Existing User" });
        await dbContext.SaveChangesAsync();

        var provisioningServiceMock = Substitute.For<ITenantProvisioningService>();
        var handler = new RegisterCommandHandler(dbContext, provisioningServiceMock);

        var command = new RegisterCommand(
            FullName: "New User",
            Email: "newuser@propertyos.com",
            Phone: existingPhone,
            Password: "SecurePassword123!",
            CompanyName: "New Company"
        );

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicatePhoneException>()
            .WithMessage($"*{existingPhone}*");
    }

    [Fact]
    public async Task Handle_Should_Validate_Strategy_And_Delegate_To_ProvisioningService_On_Success()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();

        var provisioningServiceMock = Substitute.For<ITenantProvisioningService>();
        var handler = new RegisterCommandHandler(dbContext, provisioningServiceMock);

        var command = new RegisterCommand(
            FullName: "New Admin",
            Email: "admin@newcompany.com",
            Phone: "+962799999999",
            Password: "SecurePassword123!",
            CompanyName: "New Real Estate Co"
        );

        var expectedResponse = new RegisterResponseDto
        {
            RegistrationId = Guid.NewGuid(),
            Status = "Pending",
            SubmittedAt = DateTimeOffset.UtcNow,
            Message = "Pending approval"
        };

        provisioningServiceMock.ProvisionTenantAsync(command, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        await provisioningServiceMock.Received(1).ProvisionTenantAsync(command, Arg.Any<CancellationToken>());
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task TenantProvisioningService_Should_Execute_Steps_In_Correct_Order()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var executionLog = new List<string>();

        var step10 = Substitute.For<ITenantProvisioningStep>();
        step10.Order.Returns(10);
        step10.ExecuteAsync(Arg.Any<TenantProvisioningContext>(), Arg.Any<CancellationToken>())
            .Returns(call => { executionLog.Add("Step 10"); return Task.CompletedTask; });

        var step50 = Substitute.For<ITenantProvisioningStep>();
        step50.Order.Returns(50);
        step50.ExecuteAsync(Arg.Any<TenantProvisioningContext>(), Arg.Any<CancellationToken>())
            .Returns(call => { executionLog.Add("Step 50"); return Task.CompletedTask; });

        var step30 = Substitute.For<ITenantProvisioningStep>();
        step30.Order.Returns(30);
        step30.ExecuteAsync(Arg.Any<TenantProvisioningContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                executionLog.Add("Step 30");
                var ctx = call.Arg<TenantProvisioningContext>();
                ctx.Response = new RegisterResponseDto { RegistrationId = Guid.NewGuid(), Status = "Pending" };
                return Task.CompletedTask;
            });

        var service = new TenantProvisioningService(new[] { step50, step10, step30 }, dbContext);
        var command = new RegisterCommand("User", "u@test.com", null, "Password123!", "Company");

        // Act
        var result = await service.ProvisionTenantAsync(command, CancellationToken.None);

        // Assert
        executionLog.Should().ContainInOrder("Step 10", "Step 30", "Step 50");
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task TenantProvisioningService_Should_Handle_Step_Failure()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();

        var failingStep = Substitute.For<ITenantProvisioningStep>();
        failingStep.Order.Returns(10);
        failingStep.ExecuteAsync(Arg.Any<TenantProvisioningContext>(), Arg.Any<CancellationToken>())
            .Returns<Task>(call => throw new InvalidOperationException("Step failed mid-execution"));

        var service = new TenantProvisioningService(new[] { failingStep }, dbContext);
        var command = new RegisterCommand("User", "u@test.com", null, "Password123!", "Company");

        // Act
        Func<Task> act = async () => await service.ProvisionTenantAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Step failed mid-execution*");
    }
}
