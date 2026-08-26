using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PropertyOS.Application.Identity;
using PropertyOS.Application.Properties.Security;
using PropertyOS.Application.UtilityBills.Security;
using PropertyOS.Application.Common.Security;
using PropertyOS.Tests.Integration.Api.Properties;
using PropertyOS.Tests.Integration.Infrastructure;

namespace PropertyOS.Tests.Integration.Api.UtilityBills;

public sealed class UtilityBillsManagementEndpointsTests : Module4ApiTestBase
{
    public UtilityBillsManagementEndpointsTests(
        PostgresTestFixture fixture,
        WebApplicationFactory<Program> factory)
        : base(fixture, factory)
    {
    }

    [Fact]
    public async Task Module12Migration_CreatesRequiredTablesEnumsAndRls()
    {
        await using var connection = new NpgsqlConnection(Fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                to_regclass('public.utility_accounts') IS NOT NULL,
                to_regclass('public.utility_bills') IS NOT NULL,
                EXISTS (SELECT 1 FROM pg_type WHERE typname = 'utility_type_enum'),
                EXISTS (SELECT 1 FROM pg_type WHERE typname = 'utility_sync_status_enum'),
                EXISTS (SELECT 1 FROM pg_type WHERE typname = 'utility_bill_status_enum'),
                (SELECT relrowsecurity AND relforcerowsecurity
                 FROM pg_class
                 WHERE oid = 'public.utility_accounts'::regclass),
                (SELECT relrowsecurity AND relforcerowsecurity
                 FROM pg_class
                 WHERE oid = 'public.utility_bills'::regclass),
                EXISTS (
                    SELECT 1
                    FROM "__EFMigrationsHistory"
                    WHERE "MigrationId" = '20260820120000_Module12_UtilityBills')
            """;

        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();

        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            reader.GetBoolean(ordinal).Should().BeTrue(
                $"Module 12 schema invariant at ordinal {ordinal} must be provisioned by its migration");
        }
    }

    [Fact]
    public async Task ManagementList_IsCompanyScopedAndReturnsTruthfulContext()
    {
        var (companyA, userA) = await CreateTestTenantAsync("UtilityA");
        var (companyB, _) = await CreateTestTenantAsync("UtilityB");
        var accountA = await SeedUtilityGraphAsync(companyA, Guid.NewGuid(), "A", 41m);
        var secondAccountA = await SeedUtilityGraphAsync(companyA, Guid.NewGuid(), "A2", 42m);
        await SeedUtilityGraphAsync(companyB, Guid.NewGuid(), "B", 99m);

        using var client = CreateClientWithPermissions(
            userA, companyA, UtilityBillsPermissions.Manage);
        using var response = await client.GetAsync(
            "/api/v1/utility-bills/accounts?pageSize=1&utilityType=Electricity");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        var item = items[0];
        var firstId = item.GetProperty("id").GetGuid();
        new[] { accountA.AccountId, secondAccountA.AccountId }.Should().Contain(firstId);
        item.GetProperty("buildingName").GetString()
            .Should().BeOneOf("Building A", "Building A2");
        item.GetProperty("unitNumber").GetString()
            .Should().BeOneOf("A-101", "A2-101");
        item.GetProperty("tenantName").GetString()
            .Should().BeOneOf("Tenant A", "Tenant A2");
        item.TryGetProperty("lastSyncErrorDetail", out _).Should().BeFalse();
        json.RootElement.GetProperty("hasMore").GetBoolean().Should().BeTrue();

        var cursor = Uri.EscapeDataString(
            json.RootElement.GetProperty("nextCursor").GetString()!);
        using var nextResponse = await client.GetAsync(
            $"/api/v1/utility-bills/accounts?pageSize=1&utilityType=Electricity&cursor={cursor}");
        nextResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var nextJson = JsonDocument.Parse(await nextResponse.Content.ReadAsStringAsync());
        var secondId = nextJson.RootElement.GetProperty("items")[0]
            .GetProperty("id").GetGuid();
        secondId.Should().NotBe(firstId);
        new[] { accountA.AccountId, secondAccountA.AccountId }.Should().Contain(secondId);
    }

    [Fact]
    public async Task CrossCompanyIds_CannotReadDeleteOrSyncAndDoNotLeakInternals()
    {
        var (companyA, _) = await CreateTestTenantAsync("OwnerA");
        var (companyB, userB) = await CreateTestTenantAsync("OwnerB");
        var accountA = await SeedUtilityGraphAsync(companyA, Guid.NewGuid(), "SECRET", 87m);

        using var clientB = CreateClientWithPermissions(
            userB, companyB, UtilityBillsPermissions.Manage);

        (await clientB.GetAsync($"/api/v1/utility-bills/accounts/{accountA.AccountId}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await clientB.GetAsync($"/api/v1/utility-bills/accounts/{accountA.AccountId}/bills"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await clientB.DeleteAsync($"/api/v1/utility-bills/accounts/{accountA.AccountId}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await clientB.PostAsJsonAsync(
            $"/api/v1/utility-bills/accounts/{accountA.AccountId}/replace",
            new { accountNumber = "1234567890" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await clientB.PostAsync(
            $"/api/v1/utility-bills/accounts/{accountA.AccountId}/sync", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var randomResponse = await clientB.GetAsync(
            $"/api/v1/utility-bills/accounts/{Guid.NewGuid()}/bills");
        randomResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = (await randomResponse.Content.ReadAsStringAsync()).ToLowerInvariant();
        body.Should().NotContain("postgres");
        body.Should().NotContain("select");
        body.Should().NotContain("connection string");
        body.Should().NotContain("stack");
    }

    [Fact]
    public async Task ManagementEndpoints_RequireAuthenticationAndManagementPermission()
    {
        using var anonymous = CreateClientWithoutToken();
        (await anonymous.GetAsync("/api/v1/utility-bills/accounts"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var (companyId, userId) = await CreateTestTenantAsync("NoUtilityPermission");
        using var client = CreateClientWithPermissions(
            userId, companyId, PropertyPermissions.Read);
        (await client.GetAsync("/api/v1/utility-bills/accounts"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Pagination_IsBoundedAndInvalidSizeUsesStableProblemDetails()
    {
        var (companyId, userId) = await CreateTestTenantAsync("Paging");
        var account = await SeedUtilityGraphAsync(companyId, Guid.NewGuid(), "PAGE", 22m);
        using var client = CreateClientWithPermissions(
            userId, companyId, UtilityBillsPermissions.Manage);

        using var accountsResponse = await client.GetAsync(
            "/api/v1/utility-bills/accounts?pageSize=101");
        accountsResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var problem = JsonDocument.Parse(
            await accountsResponse.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("title").GetString()
            .Should().Be("Validation Failed");
        problem.RootElement.TryGetProperty("errors", out _).Should().BeTrue();

        using var billsResponse = await client.GetAsync(
            $"/api/v1/utility-bills/accounts/{account.AccountId}/bills?pageSize=0");
        billsResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task BillHistory_UsesDeterministicBoundedKeysetPagination()
    {
        var (companyId, userId) = await CreateTestTenantAsync("BillPaging");
        var account = await SeedUtilityGraphAsync(companyId, Guid.NewGuid(), "BP", 22m);
        var secondBillId = await InsertBillAsync(
            companyId, account.AccountId, new DateOnly(2026, 9, 1), 33m);
        using var client = CreateClientWithPermissions(
            userId, companyId, UtilityBillsPermissions.Manage);

        using var firstResponse = await client.GetAsync(
            $"/api/v1/utility-bills/accounts/{account.AccountId}/bills?pageSize=1");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var firstJson = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
        firstJson.RootElement.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        var firstId = firstJson.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        firstId.Should().Be(secondBillId);

        var cursor = Uri.EscapeDataString(
            firstJson.RootElement.GetProperty("nextCursor").GetString()!);
        using var secondResponse = await client.GetAsync(
            $"/api/v1/utility-bills/accounts/{account.AccountId}/bills?pageSize=1&cursor={cursor}");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var secondJson = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
        secondJson.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
        secondJson.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid()
            .Should().Be(account.BillId);
        secondJson.RootElement.GetProperty("hasMore").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task TenantEndpoints_FilterByAuthenticatedTenantWithinSameCompany()
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantIsolation");
        var tenantUserA = Guid.NewGuid();
        var tenantUserB = Guid.NewGuid();
        var accountA = await SeedUtilityGraphAsync(companyId, tenantUserA, "TENANT-A", 31m);
        var accountB = await SeedUtilityGraphAsync(companyId, tenantUserB, "TENANT-B", 73m);

        using var clientA = CreateTenantClient(tenantUserA, companyId);
        using var accountsResponse = await clientA.GetAsync(
            "/api/v1/utility-bills/my/accounts");
        accountsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var accountsJson = JsonDocument.Parse(
            await accountsResponse.Content.ReadAsStringAsync());
        accountsJson.RootElement.GetArrayLength().Should().Be(1);
        accountsJson.RootElement[0].GetProperty("id").GetGuid()
            .Should().Be(accountA.AccountId);
        accountsJson.RootElement[0].GetProperty("id").GetGuid()
            .Should().NotBe(accountB.AccountId);

        using var billsResponse = await clientA.GetAsync(
            "/api/v1/utility-bills/my/bills?utilityType=Electricity&pageSize=100");
        billsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var billsJson = JsonDocument.Parse(await billsResponse.Content.ReadAsStringAsync());
        var tenantBillItems = billsJson.RootElement.GetProperty("items");
        tenantBillItems.GetArrayLength().Should().Be(1);
        tenantBillItems[0].GetProperty("amount").GetDecimal().Should().Be(31m);
        tenantBillItems[0].GetProperty("sourceAccountNumber").GetString().Should().Be("TENANT-A");
    }

    [Fact]
    public async Task TenantEndpoints_RequireCanonicalTenantPortalPermission()
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantAuthorization");
        var tenantUserId = Guid.NewGuid();
        await SeedUtilityGraphAsync(companyId, tenantUserId, "AUTH", 19m);

        using var roleOnlyClient = CreateTenantClient(tenantUserId, companyId, Array.Empty<string>());

        (await roleOnlyClient.GetAsync("/api/v1/utility-bills/my/accounts"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await roleOnlyClient.GetAsync("/api/v1/utility-bills/my/bills"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var authorizedClient = CreateTenantClient(
            tenantUserId,
            companyId,
            new[] { PlatformPermissions.TenantPortalAccess });

        (await authorizedClient.GetAsync("/api/v1/utility-bills/my/accounts"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await authorizedClient.GetAsync("/api/v1/utility-bills/my/bills"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TenantSelfLink_ExactlyOneActiveLease_CreatesOwnScopedAccount()
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantSelfLink");
        var tenantUserId = Guid.NewGuid();
        var lease = await SeedTenantWithActiveLeaseAsync(companyId, tenantUserId, "SELF");
        using var client = CreateTenantClient(tenantUserId, companyId);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 0, accountNumber = "0260004283", meterNumber = "METER-42" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accountId = json.RootElement.GetProperty("id").GetGuid();
        json.RootElement.GetProperty("utilityType").GetInt32().Should().Be(0);
        json.RootElement.GetProperty("accountNumber").GetString().Should().Be("0260004283");
        json.RootElement.TryGetProperty("lastSyncErrorDetail", out _).Should().BeFalse();

        var stored = await ReadStoredUtilityAccountAsync(accountId);
        stored.CompanyId.Should().Be(companyId);
        stored.TenantId.Should().Be(lease.TenantId);
        stored.LeaseId.Should().Be(lease.LeaseId);
        stored.ApartmentId.Should().Be(lease.ApartmentId);
    }

    [Fact]
    public async Task TenantLifecycle_ReplaceUnlinkAndExactRelink_PreservesHistoryWithoutDuplicatingAccount()
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantLifecycle");
        var userId = Guid.NewGuid();
        await SeedTenantWithActiveLeaseAsync(companyId, userId, "LIFECYCLE");
        using var client = CreateTenantClient(userId, companyId);

        using var linkedResponse = await client.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 0, accountNumber = "1234567890" });
        linkedResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var linkedJson = JsonDocument.Parse(await linkedResponse.Content.ReadAsStringAsync());
        var originalAccountId = linkedJson.RootElement.GetProperty("id").GetGuid();
        await InsertBillAsync(companyId, originalAccountId, new DateOnly(2026, 7, 1), 44m);

        using var replacedResponse = await client.PostAsJsonAsync(
            $"/api/v1/utility-bills/my/accounts/{originalAccountId}/replace",
            new { accountNumber = "0987654321", meterNumber = "M-2" });
        replacedResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        using var replacedJson = JsonDocument.Parse(await replacedResponse.Content.ReadAsStringAsync());
        var replacementId = replacedJson.RootElement.GetProperty("id").GetGuid();
        replacementId.Should().NotBe(originalAccountId);

        using var historyAfterReplace = await client.GetAsync(
            "/api/v1/utility-bills/my/bills?utilityType=Electricity&pageSize=20");
        using var historyJson = JsonDocument.Parse(await historyAfterReplace.Content.ReadAsStringAsync());
        var historicalBill = historyJson.RootElement.GetProperty("items")[0];
        historicalBill.GetProperty("utilityAccountId").GetGuid().Should().Be(originalAccountId);
        historicalBill.GetProperty("sourceAccountNumber").GetString().Should().Be("1234567890");
        historicalBill.GetProperty("isCurrentAccount").GetBoolean().Should().BeFalse();

        (await client.DeleteAsync($"/api/v1/utility-bills/my/accounts/{replacementId}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var relinkedResponse = await client.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 0, accountNumber = "1234567890" });
        relinkedResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var relinkedJson = JsonDocument.Parse(await relinkedResponse.Content.ReadAsStringAsync());
        relinkedJson.RootElement.GetProperty("id").GetGuid().Should().Be(originalAccountId,
            "an exact same-account re-link must reactivate the historical identity");

        using var finalHistory = await client.GetAsync(
            "/api/v1/utility-bills/my/bills?utilityType=Electricity&pageSize=20");
        using var finalHistoryJson = JsonDocument.Parse(await finalHistory.Content.ReadAsStringAsync());
        var finalItems = finalHistoryJson.RootElement.GetProperty("items");
        finalItems.GetArrayLength().Should().Be(1,
            "re-linking the exact account must not duplicate its persisted bills");
        finalItems[0].GetProperty("isCurrentAccount").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task TenantSelfLink_RequiresAuthenticationTenantRoleAndCanonicalPermission()
    {
        var (companyId, userId) = await CreateTestTenantAsync("TenantSelfLinkAuth");
        await SeedTenantWithActiveLeaseAsync(companyId, userId, "AUTH-LINK");
        var body = JsonContent.Create(
            new { utilityType = 0, accountNumber = "1234567890", meterNumber = (string?)null });

        using var anonymous = CreateClientWithoutToken();
        (await anonymous.PostAsync("/api/v1/utility-bills/my/accounts", body))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var roleOnly = CreateTenantClient(userId, companyId, Array.Empty<string>());
        (await roleOnly.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 0, accountNumber = "1234567890" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var nonTenant = CreateClientWithPermissions(
            userId, companyId, PlatformPermissions.TenantPortalAccess);
        (await nonTenant.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 0, accountNumber = "1234567890" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TenantSelfLink_NoActiveLease_FailsClosed()
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantNoLease");
        var userId = Guid.NewGuid();
        await SeedTenantProfileAsync(companyId, userId, "NO-LEASE");
        using var client = CreateTenantClient(userId, companyId);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 1, accountNumber = "123456" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString()
            .Should().Be("UTILITY_ACCOUNT_NO_ELIGIBLE_LEASE");
        (await CountUtilityAccountsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TenantSelfLink_MultipleActiveLeases_FailsClosedWithoutSelectingOne()
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantAmbiguousLease");
        var userId = Guid.NewGuid();
        var tenantId = await SeedTenantProfileAsync(companyId, userId, "AMBIGUOUS");
        await SeedActiveLeaseAsync(companyId, tenantId, "AMB-A");
        await SeedActiveLeaseAsync(companyId, tenantId, "AMB-B");
        using var client = CreateTenantClient(userId, companyId);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 0, accountNumber = "1234567890" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString()
            .Should().Be("UTILITY_ACCOUNT_ACTIVE_LEASE_AMBIGUOUS");
        (await CountUtilityAccountsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TenantSelfLink_RejectsOverpostedAuthorizationBoundaryIdentifiers()
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantOverpost");
        var userId = Guid.NewGuid();
        await SeedTenantWithActiveLeaseAsync(companyId, userId, "OVERPOST");
        using var client = CreateTenantClient(userId, companyId);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new
            {
                utilityType = 0,
                accountNumber = "1234567890",
                leaseContractId = Guid.NewGuid(),
                companyId = Guid.NewGuid(),
                tenantId = Guid.NewGuid()
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CountUtilityAccountsAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(2, "VALID", null)]
    [InlineData(0, "", null)]
    public async Task TenantSelfLink_InvalidInput_ReturnsControlledValidation(
        int utilityType,
        string accountNumber,
        string? meterNumber)
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantInvalidLink");
        var userId = Guid.NewGuid();
        await SeedTenantWithActiveLeaseAsync(companyId, userId, "INVALID");
        using var client = CreateTenantClient(userId, companyId);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType, accountNumber, meterNumber });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CountUtilityAccountsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TenantSelfLink_InvalidMeterNumber_ReturnsControlledValidation()
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantInvalidMeter");
        var userId = Guid.NewGuid();
        await SeedTenantWithActiveLeaseAsync(companyId, userId, "METER");
        using var client = CreateTenantClient(userId, companyId);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 1, accountNumber = "123456", meterNumber = new string('M', 101) });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CountUtilityAccountsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TenantSelfLink_ConcurrentDuplicateRequests_CreateOneRowAndReturnSafeConflict()
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantConcurrentLink");
        var userId = Guid.NewGuid();
        await SeedTenantWithActiveLeaseAsync(companyId, userId, "CONCURRENT");
        using var firstClient = CreateTenantClient(userId, companyId);
        using var secondClient = CreateTenantClient(userId, companyId);

        var first = firstClient.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 0, accountNumber = "1234567890" });
        var second = secondClient.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 0, accountNumber = "1234567890" });

        var responses = await Task.WhenAll(first, second);
        responses.Select(response => response.StatusCode)
            .Should().BeEquivalentTo(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
        (await CountUtilityAccountsAsync()).Should().Be(1);

        var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        var body = (await conflict.Content.ReadAsStringAsync()).ToLowerInvariant();
        body.Should().NotContain("postgres");
        body.Should().NotContain("sql");
        body.Should().NotContain("constraint");
        body.Should().NotContain("stack");
    }

    [Fact]
    public async Task TenantSelfLink_MismatchedCompanyContext_CannotCrossCompanyBoundary()
    {
        var (companyA, _) = await CreateTestTenantAsync("TenantCompanyA");
        var (companyB, _) = await CreateTestTenantAsync("TenantCompanyB");
        var userA = Guid.NewGuid();
        await SeedTenantWithActiveLeaseAsync(companyA, userA, "COMPANY-A");
        using var maliciousContext = CreateTenantClient(userA, companyB);

        using var response = await maliciousContext.PostAsJsonAsync(
            "/api/v1/utility-bills/my/accounts",
            new { utilityType = 0, accountNumber = "1234567890" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CountUtilityAccountsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TenantToken_CannotUseManagementLinkEndpoint()
    {
        var (companyId, _) = await CreateTestTenantAsync("TenantManagementDenied");
        var userId = Guid.NewGuid();
        var lease = await SeedTenantWithActiveLeaseAsync(companyId, userId, "DENIED");
        using var tenant = CreateTenantClient(userId, companyId);

        using var response = await tenant.PostAsJsonAsync(
            "/api/v1/utility-bills/accounts",
            new
            {
                leaseContractId = lease.LeaseId,
                utilityType = 0,
                accountNumber = "1234567890"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CountUtilityAccountsAsync()).Should().Be(0);
    }

    private HttpClient CreateTenantClient(
        Guid userId,
        Guid companyId,
        string[]? permissions = null)
    {
        var client = Factory.CreateClient();
        using var scope = Factory.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var (token, _) = tokenGenerator.GenerateAccessToken(
            userId,
            companyId,
            new[] { "TENANT" },
            permissions ?? new[] { PlatformPermissions.TenantPortalAccess });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<SeededTenantLease> SeedTenantWithActiveLeaseAsync(
        Guid companyId,
        Guid userId,
        string suffix)
    {
        var tenantId = await SeedTenantProfileAsync(companyId, userId, suffix);
        return await SeedActiveLeaseAsync(companyId, tenantId, suffix);
    }

    private async Task<Guid> SeedTenantProfileAsync(
        Guid companyId,
        Guid userId,
        string suffix)
    {
        var tenantId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(Fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO users (id, full_name, email)
            VALUES (@userId, @tenantName, @email)
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO tenants (
                id, company_id, user_id, name, national_id, phone,
                created_at, updated_at)
            VALUES (
                @tenantId, @companyId, @userId, @tenantName, @nationalId,
                @phone, now(), now());
            """;
        command.Parameters.AddWithValue("companyId", companyId);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("tenantName", $"Tenant {suffix}");
        command.Parameters.AddWithValue("email", $"tenant-link-{userId:N}@test.local");
        command.Parameters.AddWithValue("nationalId", tenantId.ToString("N")[..10]);
        command.Parameters.AddWithValue("phone", $"+96279{Random.Shared.Next(1000000, 9999999)}");
        await command.ExecuteNonQueryAsync();
        return tenantId;
    }

    private async Task<SeededTenantLease> SeedActiveLeaseAsync(
        Guid companyId,
        Guid tenantId,
        string suffix)
    {
        var buildingId = Guid.NewGuid();
        var floorId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(Fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO buildings (
                id, company_id, name, building_type, total_floors, created_at, updated_at)
            VALUES (
                @buildingId, @companyId, @buildingName,
                'residential'::building_type_enum, 1, now(), now());

            INSERT INTO floors (
                id, company_id, building_id, floor_number, floor_label,
                floor_type, created_at, updated_at)
            VALUES (
                @floorId, @companyId, @buildingId, 1, 'Floor 1',
                'regular'::floor_type_enum, now(), now());

            INSERT INTO apartments (
                id, company_id, building_id, floor_id, unit_number,
                occupancy_status, area_sqm, bedrooms, bathrooms,
                created_at, updated_at)
            VALUES (
                @apartmentId, @companyId, @buildingId, @floorId, @unitNumber,
                'vacant'::occupancy_status_enum, 80, 2, 1, now(), now());

            INSERT INTO lease_contracts (
                id, company_id, building_id, apartment_id, tenant_id,
                contract_number, start_date, end_date, monthly_rent_amount,
                payment_frequency, payment_due_day, status, created_at, updated_at)
            VALUES (
                @leaseId, @companyId, @buildingId, @apartmentId, @tenantId,
                @contractNumber, '2026-01-01', '2026-12-31', 400,
                'monthly'::payment_frequency_enum, 1,
                'active'::contract_status_enum, now(), now());
            """;
        command.Parameters.AddWithValue("companyId", companyId);
        command.Parameters.AddWithValue("buildingId", buildingId);
        command.Parameters.AddWithValue("buildingName", $"Building {suffix}");
        command.Parameters.AddWithValue("floorId", floorId);
        command.Parameters.AddWithValue("apartmentId", apartmentId);
        command.Parameters.AddWithValue("unitNumber", $"{suffix}-{apartmentId:N}"[..20]);
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("leaseId", leaseId);
        command.Parameters.AddWithValue("contractNumber", $"LEASE-{leaseId:N}");
        await command.ExecuteNonQueryAsync();
        return new SeededTenantLease(tenantId, leaseId, apartmentId);
    }

    private async Task<StoredUtilityAccount> ReadStoredUtilityAccountAsync(Guid accountId)
    {
        await using var connection = new NpgsqlConnection(Fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT company_id, tenant_id, lease_contract_id, apartment_id
            FROM utility_accounts
            WHERE id = @accountId
            """;
        command.Parameters.AddWithValue("accountId", accountId);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        return new StoredUtilityAccount(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3));
    }

    private async Task<long> CountUtilityAccountsAsync()
    {
        await using var connection = new NpgsqlConnection(Fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM utility_accounts";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<SeededUtilityGraph> SeedUtilityGraphAsync(
        Guid companyId,
        Guid tenantUserId,
        string suffix,
        decimal billAmount)
    {
        var buildingId = Guid.NewGuid();
        var floorId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var billId = Guid.NewGuid();

        await using var connection = new NpgsqlConnection(Fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO users (id, full_name, email)
            VALUES (@userId, @tenantName, @email)
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO buildings (
                id, company_id, name, building_type, total_floors, created_at, updated_at)
            VALUES (
                @buildingId, @companyId, @buildingName,
                'residential'::building_type_enum, 1, now(), now());

            INSERT INTO floors (
                id, company_id, building_id, floor_number, floor_label,
                floor_type, created_at, updated_at)
            VALUES (
                @floorId, @companyId, @buildingId, 1, 'Floor 1',
                'regular'::floor_type_enum, now(), now());

            INSERT INTO apartments (
                id, company_id, building_id, floor_id, unit_number,
                occupancy_status, area_sqm, bedrooms, bathrooms,
                created_at, updated_at)
            VALUES (
                @apartmentId, @companyId, @buildingId, @floorId, @unitNumber,
                'vacant'::occupancy_status_enum, 80, 2, 1, now(), now());

            INSERT INTO tenants (
                id, company_id, user_id, name, national_id, phone,
                created_at, updated_at)
            VALUES (
                @tenantId, @companyId, @userId, @tenantName, @nationalId,
                @phone, now(), now());

            INSERT INTO lease_contracts (
                id, company_id, building_id, apartment_id, tenant_id,
                contract_number, start_date, end_date, monthly_rent_amount,
                payment_frequency, payment_due_day, created_at, updated_at)
            VALUES (
                @leaseId, @companyId, @buildingId, @apartmentId, @tenantId,
                @contractNumber, '2026-01-01', '2026-12-31', 400,
                'monthly'::payment_frequency_enum, 1, now(), now());

            INSERT INTO utility_accounts (
                id, company_id, lease_contract_id, tenant_id, apartment_id,
                utility_type, account_number, is_active,
                historical_bootstrap_completed, next_check_at)
            VALUES (
                @accountId, @companyId, @leaseId, @tenantId, @apartmentId,
                'electricity'::utility_type_enum, @accountNumber, true, true, now());

            INSERT INTO utility_bills (
                id, company_id, utility_account_id, utility_type,
                provider_external_id, bill_date, due_date, amount, currency,
                is_paid, payment_status, is_from_historical_backfill,
                discovered_at, created_at, updated_at)
            VALUES (
                @billId, @companyId, @accountId, 'electricity'::utility_type_enum,
                @externalId, '2026-08-01', '2026-08-20', @billAmount, 'JOD',
                false, 'unpaid'::utility_bill_status_enum, false,
                now(), now(), now());
            """;
        command.Parameters.AddWithValue("companyId", companyId);
        command.Parameters.AddWithValue("userId", tenantUserId);
        command.Parameters.AddWithValue("email", $"utility-{tenantUserId:N}@test.local");
        command.Parameters.AddWithValue("buildingId", buildingId);
        command.Parameters.AddWithValue("buildingName", $"Building {suffix}");
        command.Parameters.AddWithValue("floorId", floorId);
        command.Parameters.AddWithValue("apartmentId", apartmentId);
        command.Parameters.AddWithValue("unitNumber", $"{suffix}-101");
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("tenantName", $"Tenant {suffix}");
        command.Parameters.AddWithValue("nationalId", tenantId.ToString("N")[..10]);
        command.Parameters.AddWithValue("phone", $"+96279{Random.Shared.Next(1000000, 9999999)}");
        command.Parameters.AddWithValue("leaseId", leaseId);
        command.Parameters.AddWithValue("contractNumber", $"LEASE-{leaseId:N}");
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("accountNumber", suffix);
        command.Parameters.AddWithValue("billId", billId);
        command.Parameters.AddWithValue("externalId", $"BILL-{billId:N}");
        command.Parameters.AddWithValue("billAmount", billAmount);
        await command.ExecuteNonQueryAsync();

        return new SeededUtilityGraph(accountId, billId);
    }

    private async Task<Guid> InsertBillAsync(
        Guid companyId,
        Guid accountId,
        DateOnly billDate,
        decimal amount)
    {
        var billId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(Fixture.RawConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO utility_bills (
                id, company_id, utility_account_id, utility_type,
                provider_external_id, bill_date, due_date, amount, currency,
                is_paid, payment_status, is_from_historical_backfill,
                discovered_at, created_at, updated_at)
            VALUES (
                @billId, @companyId, @accountId, 'electricity'::utility_type_enum,
                @externalId, @billDate, @billDate, @amount, 'JOD', false,
                'unpaid'::utility_bill_status_enum, false, now(), now(), now());
            """;
        command.Parameters.AddWithValue("billId", billId);
        command.Parameters.AddWithValue("companyId", companyId);
        command.Parameters.AddWithValue("accountId", accountId);
        command.Parameters.AddWithValue("externalId", $"BILL-{billId:N}");
        command.Parameters.AddWithValue("billDate", billDate);
        command.Parameters.AddWithValue("amount", amount);
        await command.ExecuteNonQueryAsync();
        return billId;
    }

    private sealed record SeededUtilityGraph(Guid AccountId, Guid BillId);
    private sealed record SeededTenantLease(Guid TenantId, Guid LeaseId, Guid ApartmentId);
    private sealed record StoredUtilityAccount(
        Guid CompanyId, Guid TenantId, Guid LeaseId, Guid ApartmentId);
}
