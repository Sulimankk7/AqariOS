using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Maintenance;
using PropertyOS.Application.Maintenance.Queries.Common;
using PropertyOS.Domain.Maintenance;
using PropertyOS.Domain.Maintenance.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure.Maintenance;

[Collection("Postgres collection")]
public class MaintenanceRequestIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _sharedAppUserConnection;

    public MaintenanceRequestIntegrationTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _sharedAppUserConnection = await _fixture.AppUserDataSource!.OpenConnectionAsync();
    }

    public async Task DisposeAsync()
    {
        if (_sharedAppUserConnection != null)
            await _sharedAppUserConnection.DisposeAsync();
    }

    private sealed class StaticTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; }
        public bool IsPlatformAdmin { get; set; }
    }

    private sealed class StaticCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; set; }
    }

    private PropertyOsDbContext CreateTenantContext(Guid? companyId)
    {
        var tenantContext = new StaticTenantContext { CompanyId = companyId, IsPlatformAdmin = false };
        var userContext = new StaticCurrentUserContext { UserId = null };

        var conn = _fixture.AppUserDataSource!.OpenConnection();

        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(conn, contextOwnsConnection: true, o =>
            {
                o.MapEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
                o.MapEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
                o.MapEnum<MaintenanceCategory>("maintenance_category_enum");
                o.MapEnum<MaintenancePriority>("maintenance_priority_enum");
                o.MapEnum<MaintenanceStatus>("maintenance_status_enum");
            })
            .AddInterceptors(new PropertyOS.Infrastructure.Persistence.Interceptors.TenantSessionInterceptor(tenantContext, userContext))
            .Options;

        return new PropertyOsDbContext(options);
    }

    private PropertyOsDbContext CreateAdminContext()
    {
        var tenantContext = new StaticTenantContext { CompanyId = null, IsPlatformAdmin = true };
        var userContext = new StaticCurrentUserContext { UserId = null };

        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(_fixture.Context.Database.GetDbConnection(), o =>
            {
                o.MapEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
                o.MapEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
                o.MapEnum<MaintenanceCategory>("maintenance_category_enum");
                o.MapEnum<MaintenancePriority>("maintenance_priority_enum");
                o.MapEnum<MaintenanceStatus>("maintenance_status_enum");
            })
            .AddInterceptors(new PropertyOS.Infrastructure.Persistence.Interceptors.TenantSessionInterceptor(tenantContext, userContext))
            .Options;

        return new PropertyOsDbContext(options);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. DATABASE CONSTRAINT TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DB_Constraints_BlankTitle_ThrowsException()
    {
        var companyId = await SeedCompanyAsync("Co 1");
        var buildingId = await SeedBuildingAsync(companyId, "Building 1");

        await using var ctx = CreateAdminContext();
        await ctx.Database.BeginTransactionAsync();

        var request = MaintenanceRequest.Create(
            companyId: companyId,
            buildingId: buildingId,
            apartmentId: null,
            tenantId: null,
            title: "Valid Temp Title",
            description: "Some description",
            category: MaintenanceCategory.Plumbing,
            priority: MaintenancePriority.Medium,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: null);
        ctx.MaintenanceRequests.Add(request);
        await ctx.SaveChangesAsync();

        // Bypass domain validation using reflection/backdoor to test database-level CHECK constraint
        var titleProp = typeof(MaintenanceRequest).GetProperty("Title");
        titleProp!.SetValue(request, "   "); // blank title bypasses C# create validation

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());
        Assert.Contains("chk_maintenance_requests_title_not_blank", ex.InnerException?.Message ?? ex.Message);
    }

    [Fact]
    public async Task DB_Constraints_FutureRequestDate_ThrowsException()
    {
        var companyId = await SeedCompanyAsync("Co 2");
        var buildingId = await SeedBuildingAsync(companyId, "Building 2");

        await using var ctx = CreateAdminContext();
        await ctx.Database.BeginTransactionAsync();

        var request = MaintenanceRequest.Create(
            companyId: companyId,
            buildingId: buildingId,
            apartmentId: null,
            tenantId: null,
            title: "Temp",
            description: "Some desc",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Low,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: null);
        ctx.MaintenanceRequests.Add(request);
        await ctx.SaveChangesAsync();

        var dateProp = typeof(MaintenanceRequest).GetProperty("RequestDate");
        dateProp!.SetValue(request, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());
        Assert.Contains("chk_maintenance_requests_request_date_not_future", ex.InnerException?.Message ?? ex.Message);
    }

    [Fact]
    public async Task DB_Constraints_ClosedStateConsistency_ThrowsException()
    {
        var companyId = await SeedCompanyAsync("Co 3");
        var buildingId = await SeedBuildingAsync(companyId, "Building 3");

        await using var ctx = CreateAdminContext();
        await ctx.Database.BeginTransactionAsync();

        var request = MaintenanceRequest.Create(
            companyId: companyId,
            buildingId: buildingId,
            apartmentId: null,
            tenantId: null,
            title: "Temp",
            description: "Some desc",
            category: MaintenanceCategory.Other,
            priority: MaintenancePriority.Low,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: null);
        ctx.MaintenanceRequests.Add(request);
        await ctx.SaveChangesAsync();

        // 1. Terminal state (Closed) but NULL closed_date -> should fail
        var statusProp = typeof(MaintenanceRequest).GetProperty("Status");
        statusProp!.SetValue(request, MaintenanceStatus.Closed);

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());
        Assert.Contains("chk_maintenance_requests_closed_date_status_consistency", ex.InnerException?.Message ?? ex.Message);
    }

    [Fact]
    public async Task DB_Constraints_DuplicateAttachments_ThrowsException()
    {
        var companyId = await SeedCompanyAsync("Co 4");
        var buildingId = await SeedBuildingAsync(companyId, "Building 4");

        await using var ctx = CreateAdminContext();
        await ctx.Database.BeginTransactionAsync();

        var request = MaintenanceRequest.Create(
            companyId: companyId,
            buildingId: buildingId,
            apartmentId: null,
            tenantId: null,
            title: "Leaking Pipe",
            description: "Some desc",
            category: MaintenanceCategory.Plumbing,
            priority: MaintenancePriority.Low,
            requestDate: DateOnly.FromDateTime(DateTime.UtcNow),
            now: DateTimeOffset.UtcNow,
            createdBy: null);
        ctx.MaintenanceRequests.Add(request);
        await ctx.SaveChangesAsync();

        var fileStorage = TestFileStorageFactory.CreateImageFileStorage(companyId, null, "photo1.jpg", "image/jpeg", 1024);
        ctx.Set<PropertyOS.Domain.Files.Entities.FileStorage>().Add(fileStorage);
        await ctx.SaveChangesAsync();

        var fileId = fileStorage.Id;
        // Client-generated child IDs: navigation discovery on a tracked parent would mark
        // the new attachment Modified, so it must be explicitly Added (mirrors the handlers).
        var firstAttachment = request.AddAttachment(fileId, null, "Photo 1", DateTimeOffset.UtcNow, null);
        ctx.MaintenanceRequestAttachments.Add(firstAttachment);
        await ctx.SaveChangesAsync();

        // Bypass domain duplicate check to insert a duplicate active attachment directly via DbContext
        var duplicateAttachment = MaintenanceRequestAttachment.Create(
            companyId: companyId,
            maintenanceRequestId: request.Id,
            fileId: fileId,
            uploadedBy: null,
            description: "Photo 2 duplicate fileId",
            now: DateTimeOffset.UtcNow,
            createdBy: null);
        ctx.MaintenanceRequestAttachments.Add(duplicateAttachment);

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());
        Assert.Contains("uq_maintenance_request_attachments_request_file", ex.InnerException?.Message ?? ex.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. RLS ISOLATION TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Rls_CrossTenantIsolation_Enforced()
    {
        var companyA = await SeedCompanyAsync("Tenant A Co");
        var companyB = await SeedCompanyAsync("Tenant B Co");

        var buildingA = await SeedBuildingAsync(companyA, "Building A");
        var buildingB = await SeedBuildingAsync(companyB, "Building B");

        Guid requestAId;
        Guid requestBId;

        // 1. Seed as Admin
        await using (var adminCtx = CreateAdminContext())
        {
            await adminCtx.Database.BeginTransactionAsync();

            var reqA = MaintenanceRequest.Create(companyA, buildingA, null, null, "Ticket A", "Desc A", MaintenanceCategory.Plumbing, MaintenancePriority.High, DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, null);
            var reqB = MaintenanceRequest.Create(companyB, buildingB, null, null, "Ticket B", "Desc B", MaintenanceCategory.Plumbing, MaintenancePriority.High, DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, null);
            
            adminCtx.MaintenanceRequests.Add(reqA);
            adminCtx.MaintenanceRequests.Add(reqB);
            await adminCtx.SaveChangesAsync();

            requestAId = reqA.Id;
            requestBId = reqB.Id;

            // Add attachments/comments/history
            var openHistA = MaintenanceStatusHistory.Create(companyA, reqA.Id, MaintenanceStatus.Open, DateTimeOffset.UtcNow, null, null, "Request opened");
            adminCtx.MaintenanceStatusHistory.Add(openHistA);

            var fsA = TestFileStorageFactory.CreateImageFileStorage(companyA, null, "picA.png");
            var fsB = TestFileStorageFactory.CreateImageFileStorage(companyB, null, "picB.png");
            adminCtx.Set<PropertyOS.Domain.Files.Entities.FileStorage>().AddRange(fsA, fsB);
            await adminCtx.SaveChangesAsync();

            var attA = reqA.AddAttachment(fsA.Id, null, "Pic A", DateTimeOffset.UtcNow, null);
            var commA = reqA.AddComment("Context A", DateTimeOffset.UtcNow, null);
            adminCtx.MaintenanceRequestAttachments.Add(attA);
            adminCtx.MaintenanceRequestComments.Add(commA);
            var histA = reqA.UpdateStatus(MaintenanceStatus.InProgress, DateTimeOffset.UtcNow, null);
            adminCtx.MaintenanceStatusHistory.Add(histA);

            var openHistB = MaintenanceStatusHistory.Create(companyB, reqB.Id, MaintenanceStatus.Open, DateTimeOffset.UtcNow, null, null, "Request opened");
            adminCtx.MaintenanceStatusHistory.Add(openHistB);

            var attB = reqB.AddAttachment(fsB.Id, null, "Pic B", DateTimeOffset.UtcNow, null);
            var commB = reqB.AddComment("Context B", DateTimeOffset.UtcNow, null);
            adminCtx.MaintenanceRequestAttachments.Add(attB);
            adminCtx.MaintenanceRequestComments.Add(commB);
            var histB = reqB.UpdateStatus(MaintenanceStatus.InProgress, DateTimeOffset.UtcNow, null);
            adminCtx.MaintenanceStatusHistory.Add(histB);

            await adminCtx.SaveChangesAsync();
            await adminCtx.Database.CommitTransactionAsync();
        }

        // 2. Query as Tenant A — should see only Request A, Attachment A, Comment A, History A
        await using (var tenantCtx = CreateTenantContext(companyA))
        {
            await tenantCtx.Database.BeginTransactionAsync();

            var queries = new PropertyOS.Infrastructure.Maintenance.Repositories.MaintenanceQueries(tenantCtx);

            var list = await queries.GetRequestsAsync(new MaintenanceRequestFilterOptions(), companyA);
            Assert.Single(list);
            Assert.Equal(requestAId, list[0].Id);

            var detail = await queries.GetDetailByIdAsync(requestAId, companyA);
            Assert.NotNull(detail);
            Assert.Equal(companyA, detail.CompanyId);

            // Cross-tenant IDOR guard check: Tenant A requesting Tenant B's request ID should return null
            var detailB = await queries.GetDetailByIdAsync(requestBId, companyA);
            Assert.Null(detailB);

            var attachments = await queries.GetAttachmentsAsync(requestAId, companyA);
            Assert.Single(attachments);

            var comments = await queries.GetCommentsAsync(requestAId, companyA);
            Assert.Single(comments);

            var history = await queries.GetStatusHistoryAsync(requestAId, companyA);
            Assert.Equal(2, history.Count); // Open -> InProgress

            await tenantCtx.Database.RollbackTransactionAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. CONCURRENCY TESTS (OPTIMISTIC CONCURRENCY VIA XMIN)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Concurrency_xminToken_ThrowsDbUpdateConcurrencyException()
    {
        var companyId = await SeedCompanyAsync("Concurrency Co");
        var buildingId = await SeedBuildingAsync(companyId, "Building 1");

        Guid requestId;

        // Seed request
        await using (var adminCtx = CreateAdminContext())
        {
            await adminCtx.Database.BeginTransactionAsync();
            var req = MaintenanceRequest.Create(companyId, buildingId, null, null, "Concurrency Ticket", "Desc", MaintenanceCategory.Other, MaintenancePriority.Low, DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, null);
            adminCtx.MaintenanceRequests.Add(req);
            await adminCtx.SaveChangesAsync();
            requestId = req.Id;
            await adminCtx.Database.CommitTransactionAsync();
        }

        // Create two independent DbContexts simulating concurrent updates
        await using var ctx1 = CreateTenantContext(companyId);
        await using var ctx2 = CreateTenantContext(companyId);

        await ctx1.Database.BeginTransactionAsync();
        await ctx2.Database.BeginTransactionAsync();

        var req1 = await ctx1.MaintenanceRequests.FirstAsync(r => r.Id == requestId);
        var req2 = await ctx2.MaintenanceRequests.FirstAsync(r => r.Id == requestId);

        // Update 1 succeeds
        var hist1 = req1.UpdateStatus(MaintenanceStatus.InProgress, DateTimeOffset.UtcNow, null);
        ctx1.MaintenanceStatusHistory.Add(hist1);
        await ctx1.SaveChangesAsync();
        await ctx1.Database.CommitTransactionAsync();

        // Update 2 fails with concurrency exception because the system xmin token changed
        var hist2 = req2.UpdateStatus(MaintenanceStatus.InProgress, DateTimeOffset.UtcNow, null);
        ctx2.MaintenanceStatusHistory.Add(hist2);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctx2.SaveChangesAsync());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. SOFT DELETE INTEGRATION TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDelete_SuppressesRecordsInQueries_ButPreservesTimelineHistory()
    {
        var companyId = await SeedCompanyAsync("Soft Delete Co");
        var buildingId = await SeedBuildingAsync(companyId, "Building");

        Guid requestId;
        Guid commentId;
        Guid attachmentId;

        // Seed request with children
        await using (var adminCtx = CreateAdminContext())
        {
            await adminCtx.Database.BeginTransactionAsync();
            var req = MaintenanceRequest.Create(companyId, buildingId, null, null, "Ticket to delete", "Desc", MaintenanceCategory.Plumbing, MaintenancePriority.Medium, DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, null);
            adminCtx.MaintenanceRequests.Add(req);
            await adminCtx.SaveChangesAsync();

            requestId = req.Id;

            var openHist = MaintenanceStatusHistory.Create(companyId, req.Id, MaintenanceStatus.Open, DateTimeOffset.UtcNow, null, null, "Request opened");
            adminCtx.MaintenanceStatusHistory.Add(openHist);

            var fs = TestFileStorageFactory.CreateImageFileStorage(companyId, null, "pic.png");
            adminCtx.Set<PropertyOS.Domain.Files.Entities.FileStorage>().Add(fs);
            await adminCtx.SaveChangesAsync();

            var att = req.AddAttachment(fs.Id, null, "Pic", DateTimeOffset.UtcNow, null);
            var comm = req.AddComment("Comment text", DateTimeOffset.UtcNow, null);
            adminCtx.MaintenanceRequestAttachments.Add(att);
            adminCtx.MaintenanceRequestComments.Add(comm);
            var hist = req.UpdateStatus(MaintenanceStatus.InProgress, DateTimeOffset.UtcNow, null);
            adminCtx.MaintenanceStatusHistory.Add(hist);

            await adminCtx.SaveChangesAsync();
            await adminCtx.Database.CommitTransactionAsync();

            attachmentId = att.Id;
            commentId = comm.Id;
        }

        // Query before soft-delete
        await using (var tenantCtx = CreateTenantContext(companyId))
        {
            await tenantCtx.Database.BeginTransactionAsync();
            var queries = new PropertyOS.Infrastructure.Maintenance.Repositories.MaintenanceQueries(tenantCtx);

            var list = await queries.GetRequestsAsync(new MaintenanceRequestFilterOptions(), companyId);
            Assert.Single(list);

            var detail = await queries.GetDetailByIdAsync(requestId, companyId);
            Assert.NotNull(detail);
            Assert.Equal(1, detail.AttachmentCount);
            Assert.Equal(1, detail.CommentCount);

            var attachments = await queries.GetAttachmentsAsync(requestId, companyId);
            Assert.Single(attachments);

            var comments = await queries.GetCommentsAsync(requestId, companyId);
            Assert.Single(comments);

            await tenantCtx.Database.RollbackTransactionAsync();
        }

        // Apply soft delete
        await using (var tenantCtx = CreateTenantContext(companyId))
        {
            await tenantCtx.Database.BeginTransactionAsync();
            var repo = new PropertyOS.Infrastructure.Maintenance.Repositories.MaintenanceRequestRepository(tenantCtx);
            var req = await repo.GetByIdAsync(requestId);
            Assert.NotNull(req);
            req.SoftDelete(DateTimeOffset.UtcNow, null);
            await tenantCtx.SaveChangesAsync();
            await tenantCtx.Database.CommitTransactionAsync();
        }

        // Query after soft-delete: Request and its child attachments/comments must be hidden
        await using (var tenantCtx = CreateTenantContext(companyId))
        {
            await tenantCtx.Database.BeginTransactionAsync();
            var queries = new PropertyOS.Infrastructure.Maintenance.Repositories.MaintenanceQueries(tenantCtx);

            var list = await queries.GetRequestsAsync(new MaintenanceRequestFilterOptions(), companyId);
            Assert.Empty(list);

            var detail = await queries.GetDetailByIdAsync(requestId, companyId);
            Assert.Null(detail);

            var attachments = await queries.GetAttachmentsAsync(requestId, companyId);
            Assert.Empty(attachments);

            var comments = await queries.GetCommentsAsync(requestId, companyId);
            Assert.Empty(comments);

            // Important: Timeline status history is append-only, and remains queryable even after request soft-delete
            var history = await queries.GetStatusHistoryAsync(requestId, companyId);
            Assert.Equal(2, history.Count); // Open -> InProgress history logs must survive!

            await tenantCtx.Database.RollbackTransactionAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS FOR DATA SEEDING
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<Guid> SeedCompanyAsync(string legalName)
    {
        var companyId = Guid.NewGuid();
        await using (var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString))
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at) 
                VALUES (@cId, @name, 'Test Co', '+962790000000', 'individual_owner', now(), now());
            ";
            cmd.Parameters.Add(new NpgsqlParameter("cId", companyId));
            cmd.Parameters.Add(new NpgsqlParameter("name", legalName));
            await cmd.ExecuteNonQueryAsync();
        }
        return companyId;
    }

    private async Task<Guid> SeedBuildingAsync(Guid companyId, string buildingName)
    {
        var buildingId = Guid.NewGuid();
        await using (var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString))
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO buildings (id, company_id, name, building_type, total_floors, created_at, updated_at) 
                VALUES (@bId, @cId, @name, 'residential', 1, now(), now());
            ";
            cmd.Parameters.Add(new NpgsqlParameter("bId", buildingId));
            cmd.Parameters.Add(new NpgsqlParameter("cId", companyId));
            cmd.Parameters.Add(new NpgsqlParameter("name", buildingName));
            await cmd.ExecuteNonQueryAsync();
        }
        return buildingId;
    }
}
