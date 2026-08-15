using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Commands.SubmitPaymentRequest;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials.Commands.SubmitPaymentRequest;

public class SubmitPaymentRequestCommandHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task Handle_AuthenticatedTenantSubmitsOwnPayment_Succeeds()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var companyId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        
        var tenant = Tenant.Create(companyId, "Test Tenant", "123", "0790000000", DateTimeOffset.UtcNow, null, userId: tenantUserId);
        var rentPayment = RentPayment.Create(companyId, Guid.NewGuid(), tenant.Id, Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        await dbContext.Tenants.AddAsync(tenant);
        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(tenantUserId);

        var handler = new SubmitPaymentRequestCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new SubmitPaymentRequestCommand(rentPayment.Id, PaymentMethod.BankTransfer, "REF123", Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedPayment = await dbContext.RentPayments.Include(rp => rp.Submissions).FirstAsync(rp => rp.Id == rentPayment.Id);
        updatedPayment.Submissions.Should().ContainSingle();
        
        var createdSubmission = updatedPayment.Submissions.Single();
        result.Should().Be(createdSubmission.Id);
        
        updatedPayment.DueDateStatus.Should().Be(DueDateStatus.PendingVerification);
    }

    [Fact]
    public async Task Handle_TenantAttemptsToSubmitAnotherTenantsPayment_Rejected()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var companyId = Guid.NewGuid();
        var attackingUserId = Guid.NewGuid();
        
        var victimTenant = Tenant.Create(companyId, "Victim", "456", "0790000001", DateTimeOffset.UtcNow, null, userId: Guid.NewGuid());
        var attackerTenant = Tenant.Create(companyId, "Attacker", "123", "0790000000", DateTimeOffset.UtcNow, null, userId: attackingUserId);
        var rentPayment = RentPayment.Create(companyId, Guid.NewGuid(), victimTenant.Id, Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        await dbContext.Tenants.AddRangeAsync(attackerTenant, victimTenant);
        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(attackingUserId);

        var handler = new SubmitPaymentRequestCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new SubmitPaymentRequestCommand(rentPayment.Id, PaymentMethod.BankTransfer, "REF123", Guid.NewGuid());

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_TenantFromAnotherCompanyAttemptsSubmission_Rejected()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var companyId1 = Guid.NewGuid();
        var companyId2 = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        
        var tenant = Tenant.Create(companyId2, "Test Tenant", "123", "0790000000", DateTimeOffset.UtcNow, null, userId: tenantUserId);
        var rentPayment = RentPayment.Create(companyId1, Guid.NewGuid(), tenant.Id, Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        await dbContext.Tenants.AddAsync(tenant);
        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(tenantUserId);

        var handler = new SubmitPaymentRequestCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new SubmitPaymentRequestCommand(rentPayment.Id, PaymentMethod.BankTransfer, "REF123", Guid.NewGuid());

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_AuthenticatedUserWithoutTenantAssociation_Rejected()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var clock = Substitute.For<IBusinessClock>();
        var publisher = Substitute.For<IPublisher>();

        var rentPayment = RentPayment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new SubmitPaymentRequestCommandHandler(dbContext, currentUser, clock, publisher);
        var command = new SubmitPaymentRequestCommand(rentPayment.Id, PaymentMethod.BankTransfer, "REF123", Guid.NewGuid());

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
