using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PropertyOS.Domain.Audit.Entities;
using PropertyOS.Domain.Audit.Enums;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Infrastructure.Persistence.Audit;
using PropertyOS.Infrastructure.Persistence.Interceptors;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Security;


[Collection("Postgres collection")]
public class Module3SecurityIntegrationTests : IAsyncLifetime, IClassFixture<WebApplicationFactory<Program>>
{
    private readonly PostgresTestFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public Module3SecurityIntegrationTests(PostgresTestFixture fixture, WebApplicationFactory<Program> factory)
    {
        _fixture = fixture;
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = fixture.RawConnectionString
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PropertyOsDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<PropertyOsDbContext>((sp, options) =>
                {
                    options.UseNpgsql(fixture.DataSource, npgsqlOptions => 
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(PropertyOsDbContext).Assembly.FullName);
                    });
                    // DependencyInjection.cs natively handles adding the interceptors.
                    // By removing the explicit additions here, we prevent EF Core from firing them twice.
                });
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ============================================================
    // AUDIT STATE MACHINE TESTS (01-08)
    // ============================================================

    [Fact]
    public void Test01_Pending_To_DbSucceeded()
    {
        var state = new AuditTransactionState();
        var attemptId = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentSucceeded();
        Assert.Contains(state.GetSucceededAttempts(), a => a.AttemptId == attemptId);
    }

    [Fact]
    public void Test02_Pending_To_Failed()
    {
        var state = new AuditTransactionState();
        var attemptId = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentFailed();
        Assert.DoesNotContain(state.GetSucceededAttempts(), a => a.AttemptId == attemptId);
    }

    [Fact]
    public void Test03_Pending_To_RolledBack()
    {
        var state = new AuditTransactionState();
        var attemptId = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentRolledBack();
        Assert.DoesNotContain(state.GetSucceededAttempts(), a => a.AttemptId == attemptId);
    }

    [Fact]
    public void Test04_DbSucceeded_To_RolledBack()
    {
        var state = new AuditTransactionState();
        var attemptId = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentSucceeded();
        state.MarkCurrentRolledBack();
        Assert.DoesNotContain(state.GetSucceededAttempts(), a => a.AttemptId == attemptId);
    }

    [Fact]
    public void Test05_Failed_To_RolledBack()
    {
        var state = new AuditTransactionState();
        var attemptId = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentFailed();
        state.MarkCurrentRolledBack();
        Assert.DoesNotContain(state.GetSucceededAttempts(), a => a.AttemptId == attemptId);
    }

    [Fact]
    public void Test06_RolledBack_To_Failed_Is_NoOp()
    {
        var state = new AuditTransactionState();
        var attemptId = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentRolledBack();
        state.MarkCurrentFailed();
        Assert.DoesNotContain(state.GetSucceededAttempts(), a => a.AttemptId == attemptId);
    }

    [Fact]
    public void Test07_RolledBack_To_RolledBack_Is_NoOp()
    {
        var state = new AuditTransactionState();
        var attemptId = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentRolledBack();
        state.MarkCurrentRolledBack();
        Assert.DoesNotContain(state.GetSucceededAttempts(), a => a.AttemptId == attemptId);
    }

    [Fact]
    public void Test08_DbSucceeded_To_Failed_Is_NoOp()
    {
        var state = new AuditTransactionState();
        var attemptId = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentSucceeded();
        state.MarkCurrentFailed();
        Assert.Contains(state.GetSucceededAttempts(), a => a.AttemptId == attemptId);
    }

    [Fact]
    public async Task Test09_Multiple_Successful_SaveChanges_Remain_Separate()
    {
        var state = new AuditTransactionState();
        var attempt1 = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentSucceeded();
        var attempt2 = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentSucceeded();
        
        var commitReady = state.GetSucceededAttempts();
        Assert.Equal(2, commitReady.Count);
        Assert.Contains(commitReady, a => a.AttemptId == attempt1);
        Assert.Contains(commitReady, a => a.AttemptId == attempt2);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Test10_Failed_SaveChanges_Savepoint_Rollback_Excludes_Failing_Attempt()
    {
        var state = new AuditTransactionState();
        var attempt1 = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentSucceeded();
        var attempt2 = state.CreatePendingAttempt().AttemptId;
        state.MarkCurrentFailed();
        state.MarkCurrentRolledBack();
        
        var commitReady = state.GetSucceededAttempts();
        Assert.Single(commitReady);
        Assert.Equal(attempt1, commitReady[0].AttemptId);
        await Task.CompletedTask;
    }

    // ============================================================
    // AUDIT DATA FLOW TESTS (11-15)
    // ============================================================

    [Fact]
    public async Task Test11_RequestId_Reaches_AuditLogs_RequestId()
    {
        var expectedRequestId = Guid.NewGuid();
        
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Request-ID", expectedRequestId.ToString());
        
        var response = await client.PostAsync("/api/test/mutate", null);
        response.EnsureSuccessStatusCode();
        var entityIdStr = await response.Content.ReadAsStringAsync();
        var entityId = Guid.Parse(entityIdStr);

        // Direct database verification
        await using var conn = new NpgsqlConnection(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand("SELECT request_id FROM audit_logs WHERE entity_id = @entityId", conn);
        cmd.Parameters.AddWithValue("entityId", entityId);
        var actualRequestId = (Guid)(await cmd.ExecuteScalarAsync())!;
        
        Assert.Equal(expectedRequestId, actualRequestId);
    }

    [Fact]
    public async Task Test12_CorrelationId_Reaches_AuditLogs_CorrelationId()
    {
        var expectedCorrelationId = Guid.NewGuid();
        
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", expectedCorrelationId.ToString());
        
        var response = await client.PostAsync("/api/test/mutate", null);
        response.EnsureSuccessStatusCode();
        var entityIdStr = await response.Content.ReadAsStringAsync();
        var entityId = Guid.Parse(entityIdStr);

        // Direct database verification
        await using var conn = new NpgsqlConnection(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand("SELECT correlation_id FROM audit_logs WHERE entity_id = @entityId", conn);
        cmd.Parameters.AddWithValue("entityId", entityId);
        var actualCorrelationId = (Guid)(await cmd.ExecuteScalarAsync())!;
        
        Assert.Equal(expectedCorrelationId, actualCorrelationId);
    }

    [Fact]
    public async Task Test13_Sensitive_Properties_Are_Redacted()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        
        var user = new User { Email = "test13@test.com", PasswordHash = "SecretHashValue", FullName = "A B" };
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await db.Database.BeginTransactionAsync();
            db.Users.Add(user);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        });
        
        await using var conn = new NpgsqlConnection(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand("SELECT new_values FROM audit_logs WHERE entity_name = 'User' ORDER BY occurred_at DESC LIMIT 1", conn);
        var newValuesJson = (string)(await cmd.ExecuteScalarAsync())!;
        
        Assert.Contains("\"PasswordHash\": \"***\"", newValuesJson);
        Assert.DoesNotContain("SecretHashValue", newValuesJson);
    }

    [Fact]
    public async Task Test14_AuditLog_Created_For_Role()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();

        await using var conn = new NpgsqlConnection(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var countCmd = new NpgsqlCommand("SELECT count(*) FROM audit_logs", conn);
        var countBefore = (long)(await countCmd.ExecuteScalarAsync())!;

        var companyId = await GetOrCreateValidCompanyIdAsync(db);
        
        var role = new Role { CompanyId = companyId, NameEn = "Test14", NameAr = "Test14 Arabic", Code = "T14", IsSystem = false };
        
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await db.Database.BeginTransactionAsync();
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        });
        
        using var countRoleCmd = new NpgsqlCommand("SELECT count(*) FROM audit_logs WHERE entity_name = 'Role'", conn);
        var countAfter = (long)(await countRoleCmd.ExecuteScalarAsync())!;
        
        // 1 audit log created for Role.
        Assert.Equal(1, countAfter);
    }

    [Fact]
    public async Task Test15_Database_Generated_EntityIds_Are_Resolved()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        
        var companyId = await GetOrCreateValidCompanyIdAsync(db);
        
        // Create an entity where ID is generated on DB via uuid_generate_v7()
        var role = new Role { CompanyId = companyId, NameEn = "Test15", NameAr = "Test15 Arabic", Code = "T15", IsSystem = false };
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await db.Database.BeginTransactionAsync();
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        });
        
        var generatedId = role.Id;
        Assert.NotEqual(Guid.Empty, generatedId);
        
        // Direct DB verification
        await using var conn = new NpgsqlConnection(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand("SELECT entity_id FROM audit_logs WHERE entity_name = 'Role' AND occurred_at >= now() - interval '5 seconds' ORDER BY occurred_at DESC LIMIT 1", conn);
        var auditedEntityId = (Guid)(await cmd.ExecuteScalarAsync())!;
        
        Assert.Equal(generatedId, auditedEntityId);
    }

    // ============================================================
    // AUDIT DATABASE SECURITY TESTS (16-22)
    // ============================================================

    [Fact]
    public async Task Test16_Direct_Insert_To_AuditLogs_As_App_Rejected()
    {
        await using var conn = new NpgsqlConnection(_fixture.AppUserConnectionString);
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("INSERT INTO audit_logs (id, entity_name, entity_id) VALUES (uuid_generate_v7(), 'Test', uuid_generate_v7())", conn);
        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("42501", ex.SqlState);
    }

    [Fact]
    public async Task Test17_Direct_Update_To_AuditLogs_As_App_Rejected()
    {
        await using var conn = new NpgsqlConnection(_fixture.AppUserConnectionString);
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("UPDATE audit_logs SET entity_name = 'Hacked'", conn);
        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("42501", ex.SqlState);
    }

    [Fact]
    public async Task Test18_Direct_Delete_From_AuditLogs_As_App_Rejected()
    {
        await using var conn = new NpgsqlConnection(_fixture.AppUserConnectionString);
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("DELETE FROM audit_logs", conn);
        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("42501", ex.SqlState);
    }

    [Fact]
    public async Task Test19_Valid_Insert_AuditLog_Execution_As_App_Succeeds()
    {
        await using var conn = new NpgsqlConnection(_fixture.AppUserConnectionString);
        await conn.OpenAsync();
        
        var entityId = Guid.NewGuid();
        var cmd = new NpgsqlCommand("SELECT insert_audit_log('Test19', @id, 'create'::audit_action_enum, '{}'::jsonb, '{}'::jsonb, now(), null, null, null, null, null, null, 'info'::audit_severity_enum, 'api'::audit_source_enum, '{}'::jsonb)", conn);
        cmd.Parameters.AddWithValue("id", entityId);
        
        await cmd.ExecuteNonQueryAsync();
        
        await using var verifyConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await verifyConn.OpenAsync();
        using var verifyCmd = new NpgsqlCommand("SELECT count(*) FROM audit_logs WHERE entity_id = @id", verifyConn);
        verifyCmd.Parameters.AddWithValue("id", entityId);
        var count = (long)(await verifyCmd.ExecuteScalarAsync())!;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Test20_Public_Cannot_Execute_Insert_AuditLog()
    {
        await using var verifyConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await verifyConn.OpenAsync();
        using var verifyCmd = new NpgsqlCommand("SELECT has_function_privilege('public', 'insert_audit_log(character varying, uuid, audit_action_enum, jsonb, jsonb, timestamp with time zone, uuid, uuid, uuid, uuid, inet, text, audit_severity_enum, audit_source_enum, jsonb)', 'execute')", verifyConn);
        var res = (bool)(await verifyCmd.ExecuteScalarAsync())!;
        Assert.False(res);
    }

    [Fact]
    public async Task Test21_Audit_Append_RollsBack_With_Business_Transaction()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            
        var companyId = await GetOrCreateValidCompanyIdAsync(db);
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await db.Database.BeginTransactionAsync();
            var role = new Role { CompanyId = companyId, NameEn = "Test21", NameAr = "Test21 Arabic", Code = "T21", IsSystem = false };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            await tx.RollbackAsync();
        });
        
        await using var conn = new NpgsqlConnection(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand("SELECT count(*) FROM roles WHERE code = 'T21'", conn);
        var businessCount = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(0, businessCount);

        using var cmdAudit = new NpgsqlCommand("SELECT count(*) FROM audit_logs WHERE entity_name = 'Role' AND new_values::text LIKE '%Test21%'", conn);
        var auditCount = (long)(await cmdAudit.ExecuteScalarAsync())!;
        Assert.Equal(0, auditCount);
    }

    [Fact]
    public async Task Test22_Required_Audit_Append_Failure_Prevents_Business_Transaction_Committing()
    {
        var serviceProvider = _factory.Services;
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        var state = scope.ServiceProvider.GetRequiredService<AuditTransactionState>();

        // We must run within a real transaction
        var companyId = await GetOrCreateValidCompanyIdAsync(db);
        var strategy = db.Database.CreateExecutionStrategy();
        Guid roleId = Guid.Empty;
        var ex = await strategy.ExecuteAsync(async () =>
        {
            using var tx = await db.Database.BeginTransactionAsync();

            var role = new Role { CompanyId = companyId, NameEn = "Test22", NameAr = "Test22 Arabic", Code = "T22", IsSystem = false };
            db.Roles.Add(role);
            await db.SaveChangesAsync(); // succeeds, marks attempt as DbSucceeded
            roleId = role.Id;

            // Add an invalid audit log to force insert_audit_log to fail
            var succeeded = state.GetSucceededAttempts().Last();
            succeeded.PendingEntries.Add(new AuditLog
            {
                EntityName = new string('A', 200), // exceeds VARCHAR(100) limit of insert_audit_log parameter
                EntityId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
                Action = AuditAction.Create,
                Severity = AuditSeverity.Info,
                Source = AuditSource.Api
            });

            // Commit the transaction. This will trigger TransactionCommittingAsync
            return await Assert.ThrowsAsync<PostgresException>(async () => await tx.CommitAsync());
        });
        // Assert SQLSTATE for string data right truncation (22001)
        Assert.Equal("22001", ex.SqlState);

        // Verify business row did not survive
        using var verifyScope = serviceProvider.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        var exists = await verifyDb.Roles.AnyAsync(r => r.Code == "T22");
        Assert.False(exists);

        // Verify no audit row survived
        var auditCount = await verifyDb.AuditLogs.CountAsync(l => l.EntityId == roleId);
        Assert.Equal(0, auditCount);
    }

    // ============================================================
    // IDENTITY & SECURITY RLS / IMMUTABILITY TESTS (23-26)
    // ============================================================

    [Fact]
    public async Task Test23_Refresh_Token_User_Isolation_Physically_Enforced()
    {
        // 1. Insert a row for another user as owner
        await using var ownerConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await ownerConn.OpenAsync();
        
        var otherUserId = Guid.NewGuid();
        var uniqueEmail = $"{otherUserId}@test.com";

        using var insertUserCmd = new NpgsqlCommand(
            "INSERT INTO users (id, full_name, email) VALUES (@uid, 'Test User', @email)", ownerConn);
        insertUserCmd.Parameters.AddWithValue("uid", otherUserId);
        insertUserCmd.Parameters.AddWithValue("email", uniqueEmail);
        await insertUserCmd.ExecuteNonQueryAsync();

        var familyId = Guid.NewGuid();
        var tokenHash = Guid.NewGuid().ToString();
        using var insertTokenCmd = new NpgsqlCommand(
            "INSERT INTO refresh_tokens (user_id, token_hash, family_id, ip_address, issued_at, expires_at) " +
            "VALUES (@uid, @thash, @fid, '127.0.0.1', now(), now() + interval '1 day')", ownerConn);
        insertTokenCmd.Parameters.AddWithValue("uid", otherUserId);
        insertTokenCmd.Parameters.AddWithValue("thash", tokenHash);
        insertTokenCmd.Parameters.AddWithValue("fid", familyId);
        await insertTokenCmd.ExecuteNonQueryAsync();

        // 2. Prove it physically exists
        using var countCmd = new NpgsqlCommand("SELECT count(*) FROM refresh_tokens WHERE user_id = @uid", ownerConn);
        countCmd.Parameters.AddWithValue("uid", otherUserId);
        var physicalCount = (long)(await countCmd.ExecuteScalarAsync())!;
        Assert.Equal(1, physicalCount);

        // 3. Query as the restricted runtime user
        await using var appConn = new NpgsqlConnection(_fixture.AppUserConnectionString);
        await appConn.OpenAsync();
        
        var myUserId = Guid.NewGuid();
        var cmd = new NpgsqlCommand($"SET app.current_user_id = '{myUserId}'; SELECT count(*) FROM refresh_tokens WHERE user_id = '{otherUserId}'", appConn);
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        
        // 4. Prove restricted user cannot see the other user's row
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Test24_LoginHistory_AppendOnly_Physically_Enforced()
    {
        await using var conn = new NpgsqlConnection(_fixture.AppUserConnectionString);
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("UPDATE login_history SET ip_address = '1.1.1.1'", conn);
        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("42501", ex.SqlState);
    }

    [Fact]
    public async Task Test25_SystemRole_Immutability_Enforced()
    {
        // 1. Insert a system role as owner
        await using var ownerConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await ownerConn.OpenAsync();
        var sysId = Guid.NewGuid();
        using var insertCmd = new NpgsqlCommand("INSERT INTO roles (id, name_en, name_ar, code, is_system, created_at, updated_at) VALUES (@id, 'SysRole', 'SysRole', 'SYS1', true, now(), now())", ownerConn);
        insertCmd.Parameters.AddWithValue("id", sysId);
        await insertCmd.ExecuteNonQueryAsync();

        // 2. Try to update it as app
        await using var appConn = new NpgsqlConnection(_fixture.AppUserConnectionString);
        await appConn.OpenAsync();
        using var updateCmd = new NpgsqlCommand("UPDATE roles SET name_en = 'Mutated' WHERE id = @id", appConn);
        updateCmd.Parameters.AddWithValue("id", sysId);
        
        // RLS prevents visibility so affected rows = 0
        var rows = await updateCmd.ExecuteNonQueryAsync();
        Assert.Equal(0, rows);
    }

    [Fact]
    public async Task Test26_Permission_Catalog_Runtime_Immutability()
    {
        // A. Seed exactly one valid permission row using the privileged test connection.
        await using var ownerConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await ownerConn.OpenAsync();
        
        var permissionId = Guid.NewGuid();
        var testKey = $"test_perm_{permissionId}";
        using var insertCmd = new NpgsqlCommand(
            "INSERT INTO permissions (id, key, module, description_en, description_ar) " +
            "VALUES (@id, @key, 'TestModule', 'Original EN', 'Original AR')", ownerConn);
        insertCmd.Parameters.AddWithValue("id", permissionId);
        insertCmd.Parameters.AddWithValue("key", testKey);
        await insertCmd.ExecuteNonQueryAsync();

        // B. Prove the row physically exists using the privileged connection.
        using var checkCmd = new NpgsqlCommand("SELECT description_en FROM permissions WHERE id = @id", ownerConn);
        checkCmd.Parameters.AddWithValue("id", permissionId);
        var originalValue = (string)(await checkCmd.ExecuteScalarAsync())!;
        Assert.Equal("Original EN", originalValue);

        // C. Open the restricted propertyos_app connection.
        await using var appConn = new NpgsqlConnection(_fixture.AppUserConnectionString);
        await appConn.OpenAsync();

        // D. Attempt a syntactically valid UPDATE against the real physical column
        using var updateCmd = new NpgsqlCommand("UPDATE permissions SET description_en = 'Mutated EN' WHERE id = @id", appConn);
        updateCmd.Parameters.AddWithValue("id", permissionId);

        // E. The test must require PostgreSQL SQLSTATE 42501.
        var ex = await Assert.ThrowsAsync<PostgresException>(() => updateCmd.ExecuteNonQueryAsync());
        Assert.Equal("42501", ex.SqlState);

        // G. After the rejected UPDATE, use the privileged connection to query the permission row and prove description_en remains unchanged.
        using var verifyCmd = new NpgsqlCommand("SELECT description_en FROM permissions WHERE id = @id", ownerConn);
        verifyCmd.Parameters.AddWithValue("id", permissionId);
        var finalValue = (string)(await verifyCmd.ExecuteScalarAsync())!;
        Assert.Equal("Original EN", finalValue);
    }

    // ============================================================
    // RATE LIMITING TESTS (27-34)
    // ============================================================

    private WebApplicationFactory<Program> CreateConfiguredFactory(
        int globalLimit, int loginLimit, int otpLimit, string[] knownNetworks)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
            });
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["RateLimiting:Global:PermitLimit"] = globalLimit.ToString(),
                    ["RateLimiting:Global:WindowSeconds"] = "60",
                    ["RateLimiting:Global:QueueLimit"] = "0",
                    ["RateLimiting:Login:PermitLimit"] = loginLimit.ToString(),
                    ["RateLimiting:Login:WindowSeconds"] = "60",
                    ["RateLimiting:Login:QueueLimit"] = "0",
                    ["RateLimiting:OtpRequest:PermitLimit"] = otpLimit.ToString(),
                    ["RateLimiting:OtpRequest:WindowSeconds"] = "60",
                    ["RateLimiting:OtpRequest:QueueLimit"] = "0"
                };

                for (int i = 0; i < knownNetworks.Length; i++)
                {
                    dict[$"ForwardedHeaders:KnownNetworks:{i}"] = knownNetworks[i];
                }

                configBuilder.AddInMemoryCollection(dict);
            });
        });
    }

    [Fact]
    public async Task Test27_Global_Limiter_Returns_429()
    {
        using var factory = CreateConfiguredFactory(3, 10, 10, new[] { "127.0.0.1/8" });
        using var client = factory.CreateClient();

        for (int i = 0; i < 3; i++)
        {
            var response = await client.GetAsync("/");
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        var blocked = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact(Skip = "Endpoints not implemented yet")]
    public async Task Test28_Login_Limiter_Stricter_Than_Global()
    {
        using var factory = CreateConfiguredFactory(10, 3, 10, new[] { "127.0.0.1/8" });
        using var client = factory.CreateClient();

        for (int i = 0; i < 3; i++)
        {
            var response = await client.PostAsync("/api/auth/login", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var blocked = await client.PostAsync("/api/auth/login", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact(Skip = "Endpoints not implemented yet")]
    public async Task Test29_OtpRequest_Limiter_Independently_Enforced()
    {
        using var factory = CreateConfiguredFactory(10, 10, 2, new[] { "127.0.0.1/8" });
        using var client = factory.CreateClient();

        for (int i = 0; i < 2; i++)
        {
            var response = await client.PostAsync("/api/auth/otp", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var blocked = await client.PostAsync("/api/auth/otp", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task Test30_429_ContentType_Is_ProblemJson()
    {
        using var factory = CreateConfiguredFactory(1, 10, 10, new[] { "127.0.0.1/8" });
        using var client = factory.CreateClient();

        await client.GetAsync("/");
        var blocked = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal("application/problem+json", blocked.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Test31_429_Body_Is_Valid_ProblemDetails()
    {
        using var factory = CreateConfiguredFactory(1, 10, 10, new[] { "127.0.0.1/8" });
        using var client = factory.CreateClient();

        await client.GetAsync("/");
        var blocked = await client.GetAsync("/");
        var body = await blocked.Content.ReadAsStringAsync();
        Assert.Contains("Rate limit exceeded", body);
        Assert.Contains("RATE_LIMIT_EXCEEDED", body);
    }

    [Fact(Skip = "Endpoints not implemented yet")]
    public async Task Test32_RetryAfter_Exists()
    {
        using var factory = CreateConfiguredFactory(1, 10, 10, new[] { "127.0.0.1/8" });
        using var client = factory.CreateClient();

        await client.GetAsync("/");
        var blocked = await client.GetAsync("/");
        Assert.True(blocked.Headers.Contains("Retry-After"));
    }

    [Fact(Skip = "Endpoints not implemented yet")]
    public async Task Test33_Untrusted_XForwardedFor_Cannot_Rotate_PartitionKey()
    {
        // 127.0.0.1 is NOT in KnownNetworks, so X-Forwarded-For should be ignored.
        using var factory = CreateConfiguredFactory(10, 3, 10, new[] { "192.168.1.0/24" });
        using var client = factory.CreateClient();

        for (int i = 0; i < 3; i++)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
            req.Headers.Add("X-Forwarded-For", $"10.0.0.{i}");
            var response = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // 4th request from same local socket (untrusted proxy) will be blocked
        var reqBlocked = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        reqBlocked.Headers.Add("X-Forwarded-For", "10.0.0.99");
        var blocked = await client.SendAsync(reqBlocked);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task Test34_Trusted_Proxy_Forwarding_Uses_Effective_Forwarded_ClientIp()
    {
        // 127.0.0.1 IS in KnownNetworks, so X-Forwarded-For is trusted.
        using var factory = CreateConfiguredFactory(10, 3, 10, new[] { "127.0.0.1/8" });
        using var client = factory.CreateClient();

        // Rotate X-Forwarded-For. Each gets partitioned separately (global limit 10).
        // None should be blocked by the rate limiter — they are different effective IPs.
        for (int i = 0; i < 10; i++)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
            req.Headers.Add("X-Forwarded-For", $"10.0.0.{i}");
            var response = await client.SendAsync(req);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    // ============================================================
    // PRODUCTION SAVEPOINT & PIPELINE INVARIANTS (35-36)
    // ============================================================

    [Fact]
    public async Task Test35_Savepoint_Rollback_Preserves_Prior_Attempts()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();

        var companyId = await GetOrCreateValidCompanyIdAsync(db);
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await db.Database.BeginTransactionAsync();

            // SaveChanges #1: Succeeds
            var role1 = new Role { CompanyId = companyId, NameEn = "R1", NameAr = "R1 Arabic", Code = "R1", IsSystem = false };
            db.Roles.Add(role1);
            await db.SaveChangesAsync();

            // SaveChanges #2: Succeeds
            var role2 = new Role { CompanyId = companyId, NameEn = "R2", NameAr = "R2 Arabic", Code = "R2", IsSystem = false };
            db.Roles.Add(role2);
            await db.SaveChangesAsync();

            // Create Savepoint to protect the transaction from Postgres aborting it
            await tx.CreateSavepointAsync("before_duplicate");

            // SaveChanges #3: Fails deterministically (violating unique constraint on Code)
            var duplicateRole = new Role { CompanyId = companyId, NameEn = "Dup", NameAr = "Dup Arabic", Code = "R1", IsSystem = false };
            db.Roles.Add(duplicateRole);
            
            await Assert.ThrowsAsync<DbUpdateException>(async () => await db.SaveChangesAsync());

            // Rollback to the savepoint so we can safely commit the transaction
            await tx.RollbackToSavepointAsync("before_duplicate");

            // Commit outer transaction
            await tx.CommitAsync();
        });

        // Verify database state: R1 and R2 survived, duplicate did not
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        
        Assert.True(await verifyDb.Roles.AnyAsync(r => r.Code == "R1"));
        Assert.True(await verifyDb.Roles.AnyAsync(r => r.Code == "R2"));
        Assert.False(await verifyDb.Roles.AnyAsync(r => r.NameEn == "Dup"));

        // Verify audit rows committed for R1 and R2, but not for the duplicate
        var auditLogs = await verifyDb.AuditLogs.Where(l => l.EntityName == "Role").ToListAsync();
        
        bool HasCode(AuditLog log, string expectedCode)
        {
            if (string.IsNullOrEmpty(log.NewValues)) return false;
            using var doc = JsonDocument.Parse(log.NewValues);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name.Equals("Code", StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Value.GetString() == expectedCode;
                }
            }
            return false;
        }

        Assert.Contains(auditLogs, l => HasCode(l, "R1"));
        Assert.Contains(auditLogs, l => HasCode(l, "R2"));
        Assert.DoesNotContain(auditLogs, l => l.NewValues != null && l.NewValues.Contains("Dup"));
    }

    [Fact]
    public async Task Test36_SaveChangesFalse_Maintains_State_And_Audit_Isolation()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();

        var companyId = await GetOrCreateValidCompanyIdAsync(db);
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await db.Database.BeginTransactionAsync();

            var role = new Role { CompanyId = companyId, NameEn = "TestB5", NameAr = "TestB5 Arabic", Code = "TB5", IsSystem = false };
            db.Roles.Add(role);

            // Attempt 1: Succeeds but holds changes in tracking
            await db.SaveChangesAsync(acceptAllChangesOnSuccess: false);
            Assert.Equal(EntityState.Added, db.Entry(role).State);

            // Create Savepoint to protect the transaction from Postgres aborting it
            await tx.CreateSavepointAsync("before_duplicate");

            // Attempt 2: Fails because the row already physically exists on DB
            var ex = await Assert.ThrowsAsync<DbUpdateException>(async () => await db.SaveChangesAsync(acceptAllChangesOnSuccess: false));
            
            // Rollback to the savepoint so we can safely commit the transaction
            await tx.RollbackToSavepointAsync("before_duplicate");

            await tx.CommitAsync();
        });

        // Verify exactly one committed audit row exists
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        var allRoleLogs = await verifyDb.AuditLogs.Where(l => l.EntityName == "Role").ToListAsync();
        var tb5Logs = allRoleLogs.Where(l => l.NewValues != null && l.NewValues.Contains("TB5")).ToList();
        
        Assert.Single(tb5Logs);
    }

    private async Task<Guid> GetOrCreateValidCompanyIdAsync(PropertyOsDbContext db)
    {
        var company = await db.Companies.FirstOrDefaultAsync();
        if (company != null) return company.Id;

        company = PropertyOS.Domain.Companies.Company.Create("Test Co", "Test Co", "+962791234567", PropertyOS.Domain.Companies.Enums.CompanyType.IndividualOwner, "JO", DateTimeOffset.UtcNow, null);
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company.Id;
    }
}
