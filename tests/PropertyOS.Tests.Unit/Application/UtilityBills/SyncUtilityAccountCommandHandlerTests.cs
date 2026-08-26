using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Notifications.Commands.CreateNotification;
using PropertyOS.Application.UtilityBills;
using PropertyOS.Application.UtilityBills.Commands.SyncUtilityAccount;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Notifications.Enums;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.UtilityBills;

public class SyncUtilityAccountCommandHandlerTests
{
    private readonly PropertyOsDbContext _dbContext;
    private readonly IUtilityAccountRepository _accountRepo;
    private readonly IUtilityBillRepository _billRepo;
    private readonly IUtilityBillingProvider _electricityProvider;
    private readonly IUtilityBillingProvider _waterProvider;
    private readonly IOptionsSnapshot<UtilityBillsOptions> _opts;
    private readonly IBusinessClock _clock;
    private readonly ISender _sender;
    private readonly ILogger<SyncUtilityAccountCommandHandler> _logger;
    private readonly SyncUtilityAccountCommandHandler _handler;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _tenantUserId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 5, 2, 7, 0, 0, TimeSpan.Zero);

    public SyncUtilityAccountCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new PropertyOsDbContext(options);

        _accountRepo = Substitute.For<IUtilityAccountRepository>();
        _billRepo = Substitute.For<IUtilityBillRepository>();
        _electricityProvider = Substitute.For<IUtilityBillingProvider>();
        _electricityProvider.ProviderType.Returns(UtilityType.Electricity);
        _waterProvider = Substitute.For<IUtilityBillingProvider>();
        _waterProvider.ProviderType.Returns(UtilityType.Water);
        _opts = Substitute.For<IOptionsSnapshot<UtilityBillsOptions>>();
        _clock = Substitute.For<IBusinessClock>();
        _sender = Substitute.For<ISender>();
        _logger = Substitute.For<ILogger<SyncUtilityAccountCommandHandler>>();

        _opts.Value.Returns(new UtilityBillsOptions
        {
            StaleClaimThresholdMinutes = 10,
            Electricity = new ElectricityOptions
            {
                Enabled = true,
                BatchSize = 10,
                ProviderTimeoutSeconds = 30,
                RetryHoursWithinWindow = 6,
                BillingWindowStartHour = 7,
                MaxConsecutiveFailuresBeforeSkip = 5
            },
            Water = new WaterOptions
            {
                Enabled = true,
                BatchSize = 5,
                ProviderTimeoutSeconds = 30,
                SafetyLeadDays = 3,
                DefaultCheckIntervalDays = 30,
                MaxConsecutiveFailuresBeforeSkip = 3
            }
        });

        _clock.UtcNow.Returns(_now);

        _handler = new SyncUtilityAccountCommandHandler(
            _dbContext,
            _accountRepo,
            _billRepo,
            new[] { _electricityProvider, _waterProvider },
            _opts,
            _clock,
            _sender,
            _logger);
    }


    [Fact]
    public async Task Handle_AccountAlreadyClaimed_ReturnsWithoutCallingProvider()
    {
        var accountId = Guid.NewGuid();

        _accountRepo.TryClaimForSyncAsync(accountId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((UtilityAccount?)null); // Claim failed / already locked

        await _handler.Handle(new SyncUtilityAccountCommand(accountId), CancellationToken.None);

        await _electricityProvider.DidNotReceiveWithAnyArgs()
            .FetchBillsAsync(default!, default, default, default);
    }

    [Fact]
    public async Task Handle_ProviderSuccess_NewUnpaidBill_PersistsBillAndSendsNotification()
    {
        var tenant = Tenant.Create(
            companyId: _companyId,
            name: "Ahmad Ali",
            nationalId: "9991234567",
            phone: "0791234567",
            createdAt: _now,
            createdBy: null);
        tenant.LinkUser(_tenantUserId);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var account = UtilityAccount.Create(
            companyId: _companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: tenant.Id,
            apartmentId: Guid.NewGuid(),
            utilityType: UtilityType.Electricity,
            accountNumber: "ELEC-100",
            meterNumber: null,
            createdAt: _now,
            createdBy: null);

        _accountRepo.TryClaimForSyncAsync(account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        var billRecord = new ProviderBillRecord(
            ExternalId: "BILL-2026-05",
            BillDate: new DateOnly(2026, 5, 1),
            DueDate: new DateOnly(2026, 5, 20),
            Amount: 45.50m,
            Currency: "JOD",
            IsPaid: false,
            PaymentStatus: UtilityBillPaymentStatus.Unpaid,
            ProviderReference: "REF-XYZ");

        _electricityProvider.FetchBillsAsync(account.AccountNumber, account.MeterNumber, Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Success(
                new[] { billRecord },
                totalOutstandingBalance: 20.00m));

        UtilityBill? insertedBill = null;
        _billRepo.InsertIfNewAsync(Arg.Do<UtilityBill>(b => insertedBill = b), Arg.Any<CancellationToken>())
            .Returns(true); // new bill

        _billRepo.GetUnnotifiedUnpaidBillsAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(callInfo => insertedBill is not null ? new List<UtilityBill> { insertedBill } : new List<UtilityBill>());


        await _handler.Handle(new SyncUtilityAccountCommand(account.Id, IsBootstrapRun: false), CancellationToken.None);

        Assert.Equal(UtilitySyncStatus.Synced, account.SyncStatus);
        Assert.Equal(new DateOnly(2026, 5, 1), account.LastKnownBillDate);
        Assert.Equal(20.00m, account.TotalOutstandingBalance);
        Assert.Equal(0, account.ConsecutiveFailureCount);

        // Exactly one notification sent
        await _sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(cmd =>
                cmd.RecipientUserId == _tenantUserId &&
                cmd.NotificationType == NotificationType.UtilityBillElectricity &&
                cmd.Body.Contains("45.50")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProviderFailure_PreservesLastKnownBillDate()
    {
        var existingBillDate = new DateOnly(2026, 4, 1);
        var account = UtilityAccount.Create(
            companyId: _companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            utilityType: UtilityType.Electricity,
            accountNumber: "ELEC-100",
            meterNumber: null,
            createdAt: _now.AddDays(-30),
            createdBy: null);

        account.RecordSyncSuccess(
            existingBillDate,
            _now.AddDays(-30),
            _now,
            totalOutstandingBalance: 35.00m);

        _accountRepo.TryClaimForSyncAsync(account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        _electricityProvider.FetchBillsAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Failure("HTTP_500", "Internal Server Error"));

        await _handler.Handle(new SyncUtilityAccountCommand(account.Id), CancellationToken.None);

        Assert.Equal(UtilitySyncStatus.ProviderError, account.SyncStatus);
        Assert.Equal(1, account.ConsecutiveFailureCount);
        // CRITICAL INVARIANT: LastKnownBillDate must NOT be cleared or overwritten on failure
        Assert.Equal(existingBillDate, account.LastKnownBillDate);
        Assert.Equal(35.00m, account.TotalOutstandingBalance);

        // No notifications sent on provider failure
        await _sender.DidNotReceiveWithAnyArgs().Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HistoricalBootstrapRun_SuppressesNotifications()
    {
        var tenant = Tenant.Create(
            companyId: _companyId,
            name: "Fatima Noor",
            nationalId: "9997654321",
            phone: "0787654321",
            createdAt: _now,
            createdBy: null);
        tenant.LinkUser(_tenantUserId);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var account = UtilityAccount.Create(
            companyId: _companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: tenant.Id,
            apartmentId: Guid.NewGuid(),
            utilityType: UtilityType.Water,
            accountNumber: "WTR-200",
            meterNumber: "WMTR-1",
            createdAt: _now,
            createdBy: null);

        _accountRepo.TryClaimForSyncAsync(account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        var historicalBills = new List<ProviderBillRecord>
        {
            new("WB-1", new DateOnly(2026, 1, 1), null, 15m, "JOD", true, UtilityBillPaymentStatus.Paid, null),
            new("WB-2", new DateOnly(2026, 1, 31), null, 18m, "JOD", true, UtilityBillPaymentStatus.Paid, null),
            new("WB-3", new DateOnly(2026, 3, 2), null, 22m, "JOD", false, UtilityBillPaymentStatus.Unpaid, null)
        };

        _waterProvider.FetchBillsAsync(account.AccountNumber, account.MeterNumber, sinceDate: null, Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Success(historicalBills));

        _billRepo.InsertIfNewAsync(Arg.Any<UtilityBill>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.Handle(new SyncUtilityAccountCommand(account.Id, IsBootstrapRun: true), CancellationToken.None);

        Assert.True(account.HistoricalBootstrapCompleted);
        Assert.NotNull(account.AverageBillingIntervalDays);
        Assert.Equal(2, account.BillingIntervalSampleCount); // 2 intervals from 3 bills

        // Historical import MUST NOT send notifications
        await _sender.DidNotReceiveWithAnyArgs().Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ElectricityHistoricalBootstrap_PreservesElectricityScheduleAndLeavesWaterStatisticsEmpty()
    {
        var account = UtilityAccount.Create(
            companyId: _companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            utilityType: UtilityType.Electricity,
            accountNumber: "ELEC-HISTORY-TEST",
            meterNumber: null,
            createdAt: _now,
            createdBy: null);

        _accountRepo.TryClaimForSyncAsync(
                account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        _electricityProvider.FetchBillsAsync(
                account.AccountNumber,
                account.MeterNumber,
                sinceDate: null,
                Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Success(new List<ProviderBillRecord>
            {
                new("EB-1", new DateOnly(2026, 1, 1), null, 20m, "JOD", true, UtilityBillPaymentStatus.Paid, null),
                new("EB-2", new DateOnly(2026, 2, 1), null, 22m, "JOD", true, UtilityBillPaymentStatus.Paid, null),
                new("EB-3", new DateOnly(2026, 5, 1), null, 25m, "JOD", false, UtilityBillPaymentStatus.Unpaid, null)
            }));
        _billRepo.InsertIfNewAsync(
                Arg.Any<UtilityBill>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.Handle(
            new SyncUtilityAccountCommand(account.Id, IsBootstrapRun: true),
            CancellationToken.None);

        var ammanTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman");
        var expectedNextCheckUtc = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2026, 6, 1, 7, 0, 0, DateTimeKind.Unspecified),
            ammanTz);

        Assert.True(account.HistoricalBootstrapCompleted);
        Assert.Null(account.AverageBillingIntervalDays);
        Assert.Equal(0, account.BillingIntervalSampleCount);
        Assert.Null(account.EstimatedNextBillDate);
        Assert.Equal(new DateTimeOffset(expectedNextCheckUtc, TimeSpan.Zero), account.NextCheckAt);
        await _sender.DidNotReceiveWithAnyArgs().Send(
            Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WaterIncrementalRun_WithMultipleNewBills_LearnsEveryInterval()
    {
        var account = UtilityAccount.Create(
            companyId: _companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            utilityType: UtilityType.Water,
            accountNumber: "WTR-MULTI-1",
            meterNumber: null,
            createdAt: _now.AddMonths(-6),
            createdBy: null);

        account.MarkBootstrapCompleted(
            lastKnownBillDate: new DateOnly(2026, 3, 1),
            averageBillingIntervalDays: 30,
            sampleCount: 2,
            estimatedNextBillDate: new DateOnly(2026, 3, 31),
            syncedAt: _now.AddMonths(-2),
            nextCheckAt: _now.AddDays(-1));

        _accountRepo.TryClaimForSyncAsync(
                account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        _waterProvider.FetchBillsAsync(
                account.AccountNumber,
                account.MeterNumber,
                account.LastKnownBillDate,
                Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Success(new List<ProviderBillRecord>
            {
                new("WB-MULTI-1", new DateOnly(2026, 3, 31), null, 10m, "JOD", true, UtilityBillPaymentStatus.Paid, null),
                new("WB-MULTI-2", new DateOnly(2026, 4, 29), null, 12m, "JOD", true, UtilityBillPaymentStatus.Paid, null)
            }));

        _billRepo.InsertIfNewAsync(Arg.Any<UtilityBill>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.Handle(
            new SyncUtilityAccountCommand(account.Id, IsBootstrapRun: false),
            CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 4, 29), account.LastKnownBillDate);
        Assert.Equal((short)4, account.BillingIntervalSampleCount);
        Assert.Equal((short)30, account.AverageBillingIntervalDays);
        Assert.Equal(new DateOnly(2026, 5, 29), account.EstimatedNextBillDate);
    }

    [Fact]
    public async Task Handle_ProviderNotConfigured_LogsAndSkipsWithoutIncrementingFailureCount()
    {
        var account = UtilityAccount.Create(
            companyId: _companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            utilityType: UtilityType.Electricity,
            accountNumber: "ELEC-100",
            meterNumber: null,
            createdAt: _now,
            createdBy: null);

        _accountRepo.TryClaimForSyncAsync(account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        _electricityProvider.FetchBillsAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Failure("PROVIDER_NOT_CONFIGURED", "Provider disabled"));

        await _handler.Handle(new SyncUtilityAccountCommand(account.Id), CancellationToken.None);

        Assert.Equal(0, account.ConsecutiveFailureCount);
        Assert.Equal(_now, account.LastAttemptedSyncAt);
        Assert.Equal(UtilitySyncStatus.ProviderError, account.SyncStatus);
        Assert.Equal(_now.AddMinutes(30), account.NextCheckAt);
        Assert.Equal("Provider disabled", account.LastSyncErrorDetail);
        Assert.Null(account.ClaimedAt);
    }

    [Fact]
    public async Task Handle_NotificationIdempotency_WhenBillAlreadyNotifiedOrDuplicate_DoesNotSendDuplicateNotification()
    {
        var tenant = Tenant.Create(
            companyId: _companyId,
            name: "Ahmad Sami",
            nationalId: "9991234567",
            phone: "0791234567",
            createdAt: _now,
            createdBy: null);
        tenant.LinkUser(_tenantUserId);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var account = UtilityAccount.Create(
            companyId: _companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: tenant.Id,
            apartmentId: Guid.NewGuid(),
            utilityType: UtilityType.Electricity,
            accountNumber: "ELEC-DUP-TEST",
            meterNumber: null,
            createdAt: _now,
            createdBy: null);

        _accountRepo.TryClaimForSyncAsync(account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        var providerBills = new List<ProviderBillRecord>
        {
            new("BILL-ELEC-KNOWN", new DateOnly(2026, 5, 1), null, 40m, "JOD", false, UtilityBillPaymentStatus.Unpaid, null)
        };

        _electricityProvider.FetchBillsAsync(account.AccountNumber, account.MeterNumber, Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Success(providerBills));

        // Simulate database duplicate check: InsertIfNewAsync returns false because bill already exists
        _billRepo.InsertIfNewAsync(Arg.Any<UtilityBill>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await _handler.Handle(new SyncUtilityAccountCommand(account.Id), CancellationToken.None);

        // Assert: Zero notifications sent because bill was not newly inserted
        await _sender.DidNotReceiveWithAnyArgs().Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PreviouslyUnnotifiedUnpaidBill_RecoversNotificationOnLaterSuccessfulSync()
    {
        var tenant = Tenant.Create(
            _companyId, "Notification Recovery", "9995554433", "0795554433",
            _now, null);
        tenant.LinkUser(_tenantUserId);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var account = UtilityAccount.Create(
            _companyId, Guid.NewGuid(), tenant.Id, Guid.NewGuid(),
            UtilityType.Electricity, "ELEC-NOTIFY-RECOVERY", null, _now, null);
        _accountRepo.TryClaimForSyncAsync(
                account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        var existingBill = UtilityBill.Create(
            _companyId, account.Id, UtilityType.Electricity, "KNOWN-UNNOTIFIED",
            new DateOnly(2026, 5, 1), null, 25m, "JOD", false,
            UtilityBillPaymentStatus.Unpaid, null, false, _now.AddDays(-1));

        _electricityProvider.FetchBillsAsync(
                account.AccountNumber, account.MeterNumber,
                Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Success(Array.Empty<ProviderBillRecord>()));
        _billRepo.GetUnnotifiedUnpaidBillsAsync(
                account.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UtilityBill> { existingBill });

        await _handler.Handle(
            new SyncUtilityAccountCommand(account.Id), CancellationToken.None);

        Assert.Equal(_now, existingBill.NotificationSentAt);
        await _sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(command =>
                command.RecipientUserId == _tenantUserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProviderFailureInvariant_Timeout_PreservesLastKnownBillDate_IncrementsFailureCount_AppliesBackoff()
    {
        var existingBillDate = new DateOnly(2026, 4, 1);
        var account = UtilityAccount.Create(
            companyId: _companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            utilityType: UtilityType.Electricity,
            accountNumber: "ELEC-TIMEOUT-TEST",
            meterNumber: null,
            createdAt: _now.AddDays(-30),
            createdBy: null);

        account.RecordSyncSuccess(existingBillDate, _now.AddDays(-30), _now);

        _accountRepo.TryClaimForSyncAsync(account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        _electricityProvider.FetchBillsAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Failure("TIMEOUT", "Provider gateway timed out."));

        await _handler.Handle(new SyncUtilityAccountCommand(account.Id), CancellationToken.None);

        Assert.Equal(UtilitySyncStatus.Timeout, account.SyncStatus);
        Assert.Equal(1, account.ConsecutiveFailureCount);
        Assert.Equal(existingBillDate, account.LastKnownBillDate); // Preserved!
        Assert.True(account.NextCheckAt > _now, "NextCheckAt must move into the future using exponential backoff");
        await _sender.DidNotReceiveWithAnyArgs().Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidAccountProviderResult_MapsOnlyThatCodeToInvalidAccount()
    {
        var account = UtilityAccount.Create(
            _companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            UtilityType.Electricity, "1234567890", null, _now, null);
        _accountRepo.TryClaimForSyncAsync(
                account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _electricityProvider.FetchBillsAsync(
                account.AccountNumber, account.MeterNumber,
                Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Failure("INVALID_ACCOUNT", "Provider detail"));

        await _handler.Handle(new SyncUtilityAccountCommand(account.Id), CancellationToken.None);

        Assert.Equal(UtilitySyncStatus.InvalidAccount, account.SyncStatus);
        Assert.Equal(1, account.ConsecutiveFailureCount);
        Assert.Null(account.ClaimedAt);
    }

    [Fact]
    public async Task Handle_ElectricityMonthlyAdvance_SuccessfulDiscoveryDuringBillingWindow_AdvancesNextCheckAtToNextMonth()
    {
        var tenant = Tenant.Create(
            companyId: _companyId,
            name: "Zaid Omar",
            nationalId: "9998887776",
            phone: "0798887776",
            createdAt: _now,
            createdBy: null);
        tenant.LinkUser(_tenantUserId);
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var account = UtilityAccount.Create(
            companyId: _companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: tenant.Id,
            apartmentId: Guid.NewGuid(),
            utilityType: UtilityType.Electricity,
            accountNumber: "ELEC-ADVANCE-TEST",
            meterNumber: null,
            createdAt: _now,
            createdBy: null);

        _accountRepo.TryClaimForSyncAsync(account.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(account);
        _accountRepo.GetByIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        var providerBills = new List<ProviderBillRecord>
        {
            new("BILL-ELEC-MAY", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 25), 35m, "JOD", false, UtilityBillPaymentStatus.Unpaid, null)
        };

        _electricityProvider.FetchBillsAsync(account.AccountNumber, account.MeterNumber, Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(ProviderBillFetchResult.Success(providerBills));

        _billRepo.InsertIfNewAsync(Arg.Any<UtilityBill>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _billRepo.GetUnnotifiedUnpaidBillsAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UtilityBill>
            {
                UtilityBill.Create(_companyId, account.Id, UtilityType.Electricity, "BILL-ELEC-MAY", new DateOnly(2026, 5, 1), null, 35m, "JOD", false, UtilityBillPaymentStatus.Unpaid, null, false, _now)
            });

        // Act
        await _handler.Handle(new SyncUtilityAccountCommand(account.Id), CancellationToken.None);

        // Assert: NextCheckAt must be moved to 2026-06-01 07:00:00 (Amman timezone)
        var ammanTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman");
        var expectedNextCheckUtc = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2026, 6, 1, 7, 0, 0, DateTimeKind.Unspecified),
            ammanTz);

        Assert.Equal(new DateTimeOffset(expectedNextCheckUtc, TimeSpan.Zero), account.NextCheckAt);
        Assert.Equal(new DateOnly(2026, 5, 1), account.LastKnownBillDate);
        Assert.Equal(UtilitySyncStatus.Synced, account.SyncStatus);
    }
}
