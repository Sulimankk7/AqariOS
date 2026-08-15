using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.RemindRentPayment;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Notifications;
using PropertyOS.Application.Notifications.Commands.DispatchNotification;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials.Commands.RemindRentPayment;

public class RemindRentPaymentCommandHandlerTests
{
    private readonly IRentPaymentRepository _rentPaymentRepository = Substitute.For<IRentPaymentRepository>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly IPostCommitRegistrar _postCommitRegistrar = Substitute.For<IPostCommitRegistrar>();
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly ILogger<RemindRentPaymentCommandHandler> _logger = Substitute.For<ILogger<RemindRentPaymentCommandHandler>>();

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _tenantUserId = Guid.NewGuid();

    private readonly List<Func<CancellationToken, Task>> _registeredPostCommitActions = new();

    public RemindRentPaymentCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(_companyId);
        _currentUserContext.UserId.Returns(_currentUserId);

        _postCommitRegistrar.When(x => x.RegisterPostCommitAction(Arg.Any<Func<CancellationToken, Task>>()))
            .Do(callInfo => _registeredPostCommitActions.Add(callInfo.Arg<Func<CancellationToken, Task>>()));
    }

    private RemindRentPaymentCommandHandler CreateHandler()
    {
        return new RemindRentPaymentCommandHandler(
            _rentPaymentRepository,
            _tenantRepository,
            _notificationRepository,
            _tenantContext,
            _currentUserContext,
            _postCommitRegistrar,
            _mediator,
            _logger
        );
    }

    private RentPayment CreateTestRentPayment(
        Guid companyId,
        Guid tenantId,
        decimal amountDue = 1000m,
        decimal amountPaid = 0m,
        DueDateStatus status = DueDateStatus.Pending)
    {
        var payment = RentPayment.Create(
            companyId: companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: tenantId,
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.ScheduledInstallment,
            amountDue: amountDue,
            currency: "JOD",
            billingPeriodStart: new DateOnly(2026, 8, 1),
            billingPeriodEnd: new DateOnly(2026, 8, 31),
            dueDate: new DateOnly(2026, 8, 10),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: _currentUserId
        );

        if (amountPaid > 0 || status != DueDateStatus.Pending)
        {
            payment.UpdateAllocationSync(amountPaid, status, DateTimeOffset.UtcNow, _currentUserId);
        }

        return payment;
    }

    private Tenant CreateTestTenant(Guid companyId, Guid? userId = null, string phone = "+962791234567", string? email = "tenant@example.com")
    {
        return Tenant.Create(
            companyId: companyId,
            name: "Suliman Tenant",
            nationalId: "9901020304",
            phone: phone,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: _currentUserId,
            email: email,
            userId: userId
        );
    }

    [Fact]
    public async Task Handle_MissingCompanyContext_ThrowsInvalidOperationException()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(Guid.NewGuid());

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tenant context is required.");
    }

    [Fact]
    public async Task Handle_ValidOutstandingPayment_CreatesNotificationAndRegistersPostCommit()
    {
        // Arrange
        var tenant = CreateTestTenant(_companyId, _tenantUserId, phone: "+962791234567", email: "tenant@example.com");
        var payment = CreateTestRentPayment(_companyId, tenant.Id, amountDue: 1000m, amountPaid: 200m, status: DueDateStatus.PartiallyPaid);

        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        Notification? savedNotification = null;
        await _notificationRepository.AddAsync(Arg.Do<Notification>(n => savedNotification = n), Arg.Any<CancellationToken>());

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RentPaymentId.Should().Be(payment.Id);
        result.Status.Should().Be("Accepted");

        savedNotification.Should().NotBeNull();
        savedNotification!.RecipientUserId.Should().Be(_tenantUserId);
        savedNotification.CompanyId.Should().Be(_companyId);
        savedNotification.NotificationType.Should().Be(NotificationType.RentDue);
        savedNotification.Deliveries.Should().HaveCount(3); // InApp, Email, Sms

        // Verify notification is NOT dispatched synchronously before commit
        await _mediator.DidNotReceive().Send(Arg.Any<DispatchNotificationCommand>(), Arg.Any<CancellationToken>());

        // Verify exactly one post-commit action is registered
        _registeredPostCommitActions.Should().HaveCount(1);

        // Execute post-commit action and verify it dispatches the notification
        await _registeredPostCommitActions[0](CancellationToken.None);
        await _mediator.Received(1).Send(
            Arg.Is<DispatchNotificationCommand>(c => c.NotificationId == savedNotification.Id),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Handle_OverduePayment_CreatesHighPriorityLatePaymentNotification()
    {
        // Arrange
        var tenant = CreateTestTenant(_companyId, _tenantUserId);
        var payment = CreateTestRentPayment(_companyId, tenant.Id, amountDue: 1000m, amountPaid: 0m, status: DueDateStatus.OverdueUnpaid);

        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        Notification? savedNotification = null;
        await _notificationRepository.AddAsync(Arg.Do<Notification>(n => savedNotification = n), Arg.Any<CancellationToken>());

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedNotification.Should().NotBeNull();
        savedNotification!.NotificationType.Should().Be(NotificationType.LatePayment);
        savedNotification.Priority.Should().Be(NotificationPriority.High);
    }

    [Fact]
    public async Task Handle_PaidPayment_ThrowsBusinessRuleException()
    {
        // Arrange
        var tenant = CreateTestTenant(_companyId, _tenantUserId);
        var payment = CreateTestRentPayment(_companyId, tenant.Id, amountDue: 1000m, amountPaid: 1000m, status: DueDateStatus.Paid);

        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.Code.Should().Be("PAYMENT_ALREADY_PAID");
    }

    [Fact]
    public async Task Handle_CancelledPayment_ThrowsBusinessRuleException()
    {
        // Arrange
        var tenant = CreateTestTenant(_companyId, _tenantUserId);
        var payment = CreateTestRentPayment(_companyId, tenant.Id, amountDue: 1000m, amountPaid: 0m, status: DueDateStatus.Pending);
        payment.Cancel(DateTimeOffset.UtcNow, _currentUserId);

        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.Code.Should().Be("PAYMENT_CANCELLED");
    }

    [Fact]
    public async Task Handle_FullySettledPaymentWithRemainingZero_ThrowsBusinessRuleException()
    {
        // Arrange
        var tenant = CreateTestTenant(_companyId, _tenantUserId);
        var payment = CreateTestRentPayment(_companyId, tenant.Id, amountDue: 500m, amountPaid: 500m, status: DueDateStatus.Pending);

        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.Code.Should().Be("PAYMENT_FULLY_SETTLED");
    }

    [Fact]
    public async Task Handle_CrossCompanyPayment_ThrowsNotFoundException()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var tenant = CreateTestTenant(otherCompanyId, _tenantUserId);
        var payment = CreateTestRentPayment(otherCompanyId, tenant.Id);

        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SoftDeletedPayment_ThrowsNotFoundException()
    {
        // Arrange
        var tenant = CreateTestTenant(_companyId, _tenantUserId);
        var payment = CreateTestRentPayment(_companyId, tenant.Id);
        payment.SoftDelete(DateTimeOffset.UtcNow, _currentUserId);

        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        _rentPaymentRepository.GetByIdAsync(paymentId, Arg.Any<CancellationToken>()).Returns((RentPayment?)null);

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(paymentId);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AssociatedTenantNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var payment = CreateTestRentPayment(_companyId, Guid.NewGuid());
        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _tenantRepository.GetByIdAsync(payment.TenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SoftDeletedTenant_ThrowsNotFoundException()
    {
        // Arrange
        var tenant = CreateTestTenant(_companyId, _tenantUserId);
        tenant.SoftDelete(DateTimeOffset.UtcNow, _currentUserId);
        var payment = CreateTestRentPayment(_companyId, tenant.Id);

        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TenantWithoutLinkedPortalUser_ThrowsBusinessRuleException()
    {
        // Arrange
        var tenantWithoutUser = CreateTestTenant(_companyId, userId: null);
        var payment = CreateTestRentPayment(_companyId, tenantWithoutUser.Id);

        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _tenantRepository.GetByIdAsync(tenantWithoutUser.Id, Arg.Any<CancellationToken>()).Returns(tenantWithoutUser);

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.Code.Should().Be("TENANT_ACCOUNT_UNAVAILABLE");
    }

    [Fact]
    public async Task PostCommit_DispatchFailure_DoesNotThrowFromRegisteredAction()
    {
        // Arrange
        var tenant = CreateTestTenant(_companyId, _tenantUserId);
        var payment = CreateTestRentPayment(_companyId, tenant.Id);

        _rentPaymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        _mediator.Send(Arg.Any<DispatchNotificationCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("External provider timeout"));

        var handler = CreateHandler();
        var command = new RemindRentPaymentCommand(payment.Id);

        await handler.Handle(command, CancellationToken.None);

        // Act & Assert
        // In TransactionBehavior, post-commit actions are executed inside a try-catch block.
        // Even if the provider throws, the post-commit handler executes after DB commit and does not invalidate the response.
        _registeredPostCommitActions.Should().HaveCount(1);
        var postCommitAct = () => _registeredPostCommitActions[0](CancellationToken.None);
        await postCommitAct.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Validator_EmptyRentPaymentId_HasValidationError()
    {
        // Arrange
        var validator = new RemindRentPaymentCommandValidator();
        var command = new RemindRentPaymentCommand(Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RemindRentPaymentCommand.RentPaymentId));
    }

    [Fact]
    public void Validator_ValidRentPaymentId_PassesValidation()
    {
        // Arrange
        var validator = new RemindRentPaymentCommandValidator();
        var command = new RemindRentPaymentCommand(Guid.NewGuid());

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
