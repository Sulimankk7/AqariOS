using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PropertyOS.Application;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.ActivateLeaseContract;
using PropertyOS.Application.Marketplace;
using PropertyOS.Application.Marketplace.Commands.AddListingImage;
using PropertyOS.Application.Marketplace.Commands.ArchiveMarketplaceListing;
using PropertyOS.Application.Marketplace.Commands.CreateMarketplaceListing;
using PropertyOS.Application.Marketplace.Commands.CreateViewingRequest;
using PropertyOS.Application.Marketplace.Commands.PublishMarketplaceListing;
using PropertyOS.Application.Marketplace.Commands.ReorderListingImages;
using PropertyOS.Application.Marketplace.Commands.SetListingCoverImage;
using PropertyOS.Application.Marketplace.Commands.UpdateMarketplaceListing;
using PropertyOS.Application.Marketplace.Commands.UpdateViewingRequestStatus;
using PropertyOS.Application.Marketplace.Queries.Common;
using PropertyOS.Application.Marketplace.Queries.GetCompanyListings;
using PropertyOS.Application.Marketplace.Queries.GetListingDetails;
using PropertyOS.Application.Marketplace.Queries.GetPublicListings;
using PropertyOS.Application.Marketplace.Queries.GetViewingRequests;
using PropertyOS.Domain.Common.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Marketplace;
using PropertyOS.Domain.Marketplace.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Application.Properties;
using PropertyOS.Infrastructure.Leasing.Repositories;
using PropertyOS.Infrastructure.Marketplace.Repositories;
using PropertyOS.Infrastructure.Marketplace.Services;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Infrastructure.Properties.Repositories;
using PropertyOS.Infrastructure.Persistence.Audit;
using PropertyOS.Infrastructure.Persistence.Behaviors;
using PropertyOS.Infrastructure.Persistence.Interceptors;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Application;

[Collection("Postgres collection")]
public class MarketplaceIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private ServiceProvider _serviceProvider = null!;
    private Guid _companyId;
    private Guid _userId;
    private Guid _buildingId;
    private Guid _apartmentId;

    public MarketplaceIntegrationTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
        _companyId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        SetupDI();
        await SeedTestDataAsync();
    }

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    private void SetupDI()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_fixture.RawConnectionString);
        // Map all enums
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Subscriptions.Enums.SubscriptionStatusEnum>("subscription_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Subscriptions.Enums.BillingCycleEnum>("billing_cycle_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Audit.Enums.AuditAction>("audit_action_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSeverity>("audit_severity_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSource>("audit_source_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.LoginStatus>("login_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.MembershipStatus>("membership_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.MfaType>("mfa_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.OtpPurpose>("otp_purpose_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.RevokeReason>("revoke_reason_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.BuildingType>("building_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.Governorate>("governorate_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.FloorType>("floor_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.OwnershipStatus>("ownership_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.OccupancyStatus>("occupancy_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingType>("parking_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingAssignmentStatus>("parking_assignment_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractStatus>("contract_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.PaymentFrequency>("payment_frequency_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.TerminationType>("termination_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractDocumentType>("contract_document_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.LegalRegime>("legal_regime_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.TenantType>("tenant_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentPurpose>("payment_purpose_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentMethod>("payment_method_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.DueDateStatus>("due_date_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ChequeStatus>("cheque_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.AllocationStatus>("allocation_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ExpenseCategory>("expense_category_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ExpensePaymentMethod>("expense_payment_method_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ReceiptResetPolicy>("receipt_reset_policy_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.EfawateercomStatus>("efawateercom_status_enum");

        // Module 9 enums
        dataSourceBuilder.MapEnum<CurrencyCode>("currency_code_enum");
        dataSourceBuilder.MapEnum<ListingStatus>("listing_status_enum");
        dataSourceBuilder.MapEnum<ViewingRequestStatus>("viewing_request_status_enum");

        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);
        services.AddScoped<AuditTransactionState>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<TenantSessionInterceptor>();

        // Mock contexts
        var tenantContext = new TestTenantContext { CompanyId = _companyId };
        var currentUserContext = new TestCurrentUserContext { UserId = _userId };
        services.AddSingleton<ITenantContext>(tenantContext);
        services.AddSingleton<ICurrentUserContext>(currentUserContext);

        var auditRequestContext = new TestAuditRequestContext { RequestId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Source = PropertyOS.Domain.Audit.Enums.AuditSource.Api };
        services.AddSingleton<IAuditRequestContext>(auditRequestContext);

        services.AddDbContext<PropertyOsDbContext>((sp, options) =>
        {
            options.UseNpgsql(dataSource, o =>
            {
                o.MapEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
                o.MapEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
                o.MapEnum<PropertyOS.Domain.Subscriptions.Enums.SubscriptionStatusEnum>("subscription_status_enum");
                o.MapEnum<PropertyOS.Domain.Subscriptions.Enums.BillingCycleEnum>("billing_cycle_enum");
                o.MapEnum<PropertyOS.Domain.Audit.Enums.AuditAction>("audit_action_enum");
                o.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSeverity>("audit_severity_enum");
                o.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSource>("audit_source_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.LoginStatus>("login_status_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.MembershipStatus>("membership_status_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.MfaType>("mfa_type_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.OtpPurpose>("otp_purpose_enum");
                o.MapEnum<PropertyOS.Domain.Identity.Enums.RevokeReason>("revoke_reason_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.BuildingType>("building_type_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.Governorate>("governorate_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.FloorType>("floor_type_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.OwnershipStatus>("ownership_status_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.OccupancyStatus>("occupancy_status_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingType>("parking_type_enum");
                o.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingAssignmentStatus>("parking_assignment_status_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractStatus>("contract_status_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.PaymentFrequency>("payment_frequency_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.TerminationType>("termination_type_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractDocumentType>("contract_document_type_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.LegalRegime>("legal_regime_enum");
                o.MapEnum<PropertyOS.Domain.Leasing.Enums.TenantType>("tenant_type_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentPurpose>("payment_purpose_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentMethod>("payment_method_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.DueDateStatus>("due_date_status_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.ChequeStatus>("cheque_status_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.AllocationStatus>("allocation_status_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.ExpenseCategory>("expense_category_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.ExpensePaymentMethod>("expense_payment_method_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.ReceiptResetPolicy>("receipt_reset_policy_enum");
                o.MapEnum<PropertyOS.Domain.Financials.Enums.EfawateercomStatus>("efawateercom_status_enum");
                o.MapEnum<CurrencyCode>("currency_code_enum");
                o.MapEnum<ListingStatus>("listing_status_enum");
                o.MapEnum<ViewingRequestStatus>("viewing_request_status_enum");
            })
            .AddInterceptors(
                sp.GetRequiredService<TenantSessionInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        // Register repositories
        services.AddScoped<IMarketplaceListingRepository, MarketplaceListingRepository>();
        services.AddScoped<IViewingRequestRepository, ViewingRequestRepository>();
        services.AddScoped<IApartmentRepository, ApartmentRepository>();
        services.AddScoped<IMarketplaceQueries, MarketplaceQueries>();
        services.AddScoped<IFileStorageValidator, FileStorageValidator>();
        services.AddScoped<ILeaseContractRepository, LeaseContractRepository>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        _serviceProvider = services.BuildServiceProvider();
    }

    private async Task SeedTestDataAsync()
    {
        await using var conn = new NpgsqlConnection(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();

        // Seed Company, Building, Floor, and Apartment using SQL to bypass private constructor limitations
        cmd.CommandText = @"
            INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, country_code, created_at, updated_at, is_active, tax_number, commercial_registration_no, primary_email)
            VALUES (@companyId, 'Marketplace Management Ltd', 'Marketplace', '+962791111111', 'property_management_company'::company_type_enum, 'JO', now(), now(), true, 'TAX999', 'REG999', 'admin@mp.com');

            INSERT INTO company_settings (id, company_id, default_currency, timezone, rent_grace_period_days, late_fee_type, created_at, updated_at, default_language)
            VALUES (gen_random_uuid(), @companyId, 'JOD', 'Asia/Amman', 5, 'none'::late_fee_type_enum, now(), now(), 'en');

            INSERT INTO users (id, full_name, email, password_hash, is_active, created_at, updated_at)
            VALUES (@userId, 'Marketplace Test User', 'marketplace_admin@test.com', 'argon2id.test', true, now(), now());

            INSERT INTO buildings (id, company_id, name, building_type, total_floors, created_at, updated_at)
            VALUES (@buildingId, @companyId, 'Marketplace Plaza', 'residential'::building_type_enum, 5, now(), now());

            INSERT INTO floors (id, company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at)
            VALUES (@floorId, @companyId, @buildingId, 0, 'G', 'ground'::floor_type_enum, now(), now());

            INSERT INTO apartments (id, floor_id, building_id, company_id, unit_number, occupancy_status, bedrooms, bathrooms, base_rent_amount, area_sqm, created_at, updated_at)
            VALUES (@apartmentId, @floorId, @buildingId, @companyId, '101', 'vacant'::occupancy_status_enum, 3, 2, 500, 120.5, now(), now());
        ";

        _buildingId = Guid.NewGuid();
        _apartmentId = Guid.NewGuid();
        var floorId = Guid.NewGuid();

        cmd.Parameters.Add(new NpgsqlParameter("companyId", _companyId));
        cmd.Parameters.Add(new NpgsqlParameter("userId", _userId));
        cmd.Parameters.Add(new NpgsqlParameter("buildingId", _buildingId));
        cmd.Parameters.Add(new NpgsqlParameter("floorId", floorId));
        cmd.Parameters.Add(new NpgsqlParameter("apartmentId", _apartmentId));

        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task MarketplaceListing_LifecycleIntegrationTest_ExecutesCorrectly()
    {
        // ── Step 1: Create listing (Draft) ──────────────────────────────────
        Guid listingIdCreated;
        using (var scope = _serviceProvider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new CreateMarketplaceListingCommand(
                _apartmentId,
                "Charming Apartment",
                "Spacious 3 bedroom apartment",
                500m,
                200m,
                CurrencyCode.JOD,
                "+962790000001",
                "+962790000001",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                true
            );

            listingIdCreated = await mediator.Send(command);
            listingIdCreated.Should().NotBeEmpty();

            // Verify details
            var details = await mediator.Send(new GetListingDetailsQuery(listingIdCreated));
            details.Should().NotBeNull();
            details!.ListingTitle.Should().Be("Charming Apartment");
            details.Status.Should().Be(ListingStatus.Draft);
        }

        // ── Step 2: Add, Set Cover, and Reorder Images ──────────────────────
        using (var scope = _serviceProvider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var dbContext = scope.ServiceProvider.GetRequiredService<PropertyOS.Infrastructure.Persistence.PropertyOsDbContext>();
            var fs1 = PropertyOS.Tests.Integration.Infrastructure.TestFileStorageFactory.CreateImageFileStorage(_companyId, null, "img1.png");
            var fs2 = PropertyOS.Tests.Integration.Infrastructure.TestFileStorageFactory.CreateImageFileStorage(_companyId, null, "img2.jpg", "image/jpeg");
            dbContext.Set<PropertyOS.Domain.Files.Entities.FileStorage>().AddRange(fs1, fs2);
            await dbContext.SaveChangesAsync();

            var file1 = fs1.Id;
            var file2 = fs2.Id;

            var imgId1 = await mediator.Send(new AddListingImageCommand(listingIdCreated, file1, false));
            var imgId2 = await mediator.Send(new AddListingImageCommand(listingIdCreated, file2, false));

            imgId1.Should().NotBeEmpty();
            imgId2.Should().NotBeEmpty();

            // Set Cover
            await mediator.Send(new SetListingCoverImageCommand(listingIdCreated, imgId2));

            // Reorder
            await mediator.Send(new ReorderListingImagesCommand(listingIdCreated, new List<Guid> { imgId2, imgId1 }));

            // Verify Image State
            var details = await mediator.Send(new GetListingDetailsQuery(listingIdCreated));
            details!.Images.Should().HaveCount(2);
            details.Images[0].Id.Should().Be(imgId2); // Cover first
            details.Images[0].IsCover.Should().BeTrue();
            details.Images[1].Id.Should().Be(imgId1);
            details.Images[1].IsCover.Should().BeFalse();
        }

        // ── Step 3: Publish Listing ──────────────────────────────────────────
        using (var scope = _serviceProvider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new PublishMarketplaceListingCommand(listingIdCreated));

            var details = await mediator.Send(new GetListingDetailsQuery(listingIdCreated));
            details!.Status.Should().Be(ListingStatus.Published);
            details.PublishedDate.Should().NotBeNull();
        }

        // ── Step 4: Verify Public Access (Anonymous Select) ─────────────────
        using (var scope = _serviceProvider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Trigger public listings query
            var publicListings = await mediator.Send(new GetPublicListingsQuery(
                BuildingId: null,
                ApartmentId: null,
                SearchText: "Charming",
                LastSeenIsFeatured: null,
                LastSeenPublishedDate: null,
                LastSeenId: null,
                PageSize: 10
            ));

            publicListings.Should().HaveCount(1);
            publicListings[0].Id.Should().Be(listingIdCreated);
            publicListings[0].CoverImageFileId.Should().NotBeNull();
        }

        // ── Step 5: Viewing Request Submission ──────────────────────────────
        Guid viewingRequestId;
        using (var scope = _serviceProvider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new CreateViewingRequestCommand(
                listingIdCreated,
                "Jane Doe",
                "+962798888888",
                "jane@example.com",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                "I would love to view this apartment tomorrow."
            );

            viewingRequestId = await mediator.Send(command);
            viewingRequestId.Should().NotBeEmpty();
        }

        // ── Step 6: Viewing Request Worklist and Status Progression ─────────
        using (var scope = _serviceProvider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var list = await mediator.Send(new GetViewingRequestsQuery(
                ListingId: listingIdCreated,
                Status: ViewingRequestStatus.Pending,
                LastSeenSubmittedAt: null,
                LastSeenId: null,
                PageSize: 10
            ));

            list.Should().HaveCount(1);
            list[0].ApplicantName.Should().Be("Jane Doe");

            // Update status
            await mediator.Send(new UpdateViewingRequestStatusCommand(viewingRequestId, ViewingRequestStatus.Scheduled, "Called and scheduled"));

            // Verify status changed
            var updatedList = await mediator.Send(new GetViewingRequestsQuery(
                ListingId: listingIdCreated,
                Status: ViewingRequestStatus.Scheduled,
                LastSeenSubmittedAt: null,
                LastSeenId: null,
                PageSize: 10
            ));
            updatedList.Should().HaveCount(1);
            updatedList[0].RequestStatus.Should().Be(ViewingRequestStatus.Scheduled);
        }

        // ── Step 7: Lease Activation Reconciliation ──────────────────────────
        using (var scope = _serviceProvider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var dbContext = scope.ServiceProvider.GetRequiredService<PropertyOS.Infrastructure.Persistence.PropertyOsDbContext>();
            var contractDocFile = PropertyOS.Tests.Integration.Infrastructure.TestFileStorageFactory.CreateValidFileStorage(_companyId, null, "contract.pdf");
            dbContext.Set<PropertyOS.Domain.Files.Entities.FileStorage>().Add(contractDocFile);
            await dbContext.SaveChangesAsync();

            // Seed tenant, lease_contract, contract_document using SQL to bypass constructor restrictions
            var tenantId = Guid.NewGuid();
            var leaseContractId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await using (var conn = new NpgsqlConnection(_fixture.RawConnectionString))
            {
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO tenants (id, company_id, name, national_id, phone, created_at, updated_at)
                    VALUES (@tenantId, @companyId, 'John Doe', '1234567890', '+962790000002', now(), now());

                    INSERT INTO lease_contracts (id, company_id, building_id, apartment_id, tenant_id, contract_number, start_date, end_date, monthly_rent_amount, payment_frequency, payment_due_day, status, legal_regime, tenant_type, security_deposit_amount, created_at, updated_at)
                    VALUES (@lcId, @companyId, @buildingId, @apartmentId, @tenantId, 'LC-999', '2025-01-01', '2026-01-01', 500, 'monthly'::payment_frequency_enum, 1, 'draft'::contract_status_enum, 'standard'::legal_regime_enum, 'personal'::tenant_type_enum, 200, now(), now());

                    INSERT INTO contract_documents (id, company_id, lease_contract_id, file_id, document_type, created_at, updated_at)
                    VALUES (@docId, @companyId, @lcId, @fileId, 'signed_contract'::contract_document_type_enum, now(), now());
                ";
                cmd.Parameters.Add(new NpgsqlParameter("tenantId", tenantId));
                cmd.Parameters.Add(new NpgsqlParameter("companyId", _companyId));
                cmd.Parameters.Add(new NpgsqlParameter("buildingId", _buildingId));
                cmd.Parameters.Add(new NpgsqlParameter("apartmentId", _apartmentId));
                cmd.Parameters.Add(new NpgsqlParameter("lcId", leaseContractId));
                cmd.Parameters.Add(new NpgsqlParameter("docId", documentId));
                cmd.Parameters.Add(new NpgsqlParameter("fileId", contractDocFile.Id));

                await cmd.ExecuteNonQueryAsync();
            }

            // Activate the lease contract via handler
            await mediator.Send(new ActivateLeaseContractCommand(leaseContractId));

            // Verify that the listing is now Rented in the DB
            var details = await mediator.Send(new GetListingDetailsQuery(listingIdCreated));
            details!.Status.Should().Be(ListingStatus.Rented);
        }
    }

    private class TestTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; }
        public bool IsPlatformAdmin { get; set; } = false;
    }

    private class TestCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; set; }
    }

    private class TestAuditRequestContext : IAuditRequestContext
    {
        public Guid? RequestId { get; set; }
        public Guid? CorrelationId { get; set; }
        public PropertyOS.Domain.Audit.Enums.AuditSource Source { get; set; }
    }
}
