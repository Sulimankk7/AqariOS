using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Financials.Repositories;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials.Queries.GetPendingPaymentVerifications;

public class GetPendingPaymentVerificationsQueryHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task Handle_AuthorizedOwner_ReceivesPendingVerificationQueue()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var payment = RentPayment.Create(companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);
        payment.SubmitForVerification(PaymentMethod.BankTransfer, "REF1", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        
        dbContext.RentPayments.Add(payment);
        await dbContext.SaveChangesAsync();

        var repo = new RentPaymentRepository(dbContext);
        var handler = new GetPendingPaymentVerificationsQueryHandler(repo, tenantContext);

        // Act
        var result = await handler.Handle(new GetPendingPaymentVerificationsQuery(), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().RentPaymentId.Should().Be(payment.Id);
    }

    [Fact]
    public async Task Handle_CrossCompanyRecords_AreNotReturned()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var payment = RentPayment.Create(otherCompanyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);
        payment.SubmitForVerification(PaymentMethod.BankTransfer, "REF1", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        
        dbContext.RentPayments.Add(payment);
        await dbContext.SaveChangesAsync();

        var repo = new RentPaymentRepository(dbContext);
        var handler = new GetPendingPaymentVerificationsQueryHandler(repo, tenantContext);

        // Act
        var result = await handler.Handle(new GetPendingPaymentVerificationsQuery(), CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PendingVerificationWithoutPendingSubmission_IsExcluded()
    {
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var payment = RentPayment.Create(companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);
        // Change status to PendingVerification directly without creating a submission
        // In real domain this shouldn't happen, but we want to test the query logic
        payment.SubmitForVerification(PaymentMethod.BankTransfer, "REF1", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var submission = payment.Submissions.First();
        payment.RejectSubmission(submission.Id, "Reason", Guid.NewGuid(), DateTimeOffset.UtcNow); // this changes status back to Pending

        dbContext.RentPayments.Add(payment);
        await dbContext.SaveChangesAsync();
        
        // Force payment to be PendingVerification but no Pending submission exists
        payment.GetType().GetProperty("DueDateStatus")!.SetValue(payment, DueDateStatus.PendingVerification);
        await dbContext.SaveChangesAsync();

        var repo = new RentPaymentRepository(dbContext);
        var handler = new GetPendingPaymentVerificationsQueryHandler(repo, tenantContext);

        var result = await handler.Handle(new GetPendingPaymentVerificationsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RejectedSubmission_IsExcluded()
    {
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var payment = RentPayment.Create(companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);
        payment.SubmitForVerification(PaymentMethod.BankTransfer, "REF1", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        payment.RejectSubmission(payment.Submissions.First().Id, "Reject reason", Guid.NewGuid(), DateTimeOffset.UtcNow);
        
        dbContext.RentPayments.Add(payment);
        await dbContext.SaveChangesAsync();

        var repo = new RentPaymentRepository(dbContext);
        var handler = new GetPendingPaymentVerificationsQueryHandler(repo, tenantContext);

        var result = await handler.Handle(new GetPendingPaymentVerificationsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RentPaymentWithRejectedAndPending_ReturnsExactlyOneRow()
    {
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var payment = RentPayment.Create(companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentPurpose.ScheduledInstallment, 1000, "JOD", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);
        
        // First submission (Rejected)
        payment.SubmitForVerification(PaymentMethod.BankTransfer, "REF1", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        payment.RejectSubmission(payment.Submissions.First().Id, "Reject reason", Guid.NewGuid(), DateTimeOffset.UtcNow);
        
        // Second submission (Pending)
        payment.SubmitForVerification(PaymentMethod.BankTransfer, "REF2", null, Guid.NewGuid(), DateTimeOffset.UtcNow);

        dbContext.RentPayments.Add(payment);
        await dbContext.SaveChangesAsync();

        var repo = new RentPaymentRepository(dbContext);
        var handler = new GetPendingPaymentVerificationsQueryHandler(repo, tenantContext);

        var result = await handler.Handle(new GetPendingPaymentVerificationsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().ReferenceNumber.Should().Be("REF2");
        result.Items.First().SubmissionStatus.Should().Be(SubmissionStatus.Pending);
    }
}
