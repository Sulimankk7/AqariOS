using System;
using System.Threading.Tasks;
using FluentAssertions;
using Npgsql;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Rls;

/// <summary>
/// Module 11 RLS (doc §11.7): standard tenant isolation on all three tables, PLUS the
/// schema's one per-row restriction — a user may only read their own notifications.
/// An unset app.current_user_id is trusted system context (background jobs).
/// </summary>
[Collection("Postgres collection")]
public class NotificationsRlsTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _appConnection;

    private readonly Guid _companyA = Guid.NewGuid();
    private readonly Guid _companyB = Guid.NewGuid();
    private readonly Guid _userA1 = Guid.NewGuid();
    private readonly Guid _userA2 = Guid.NewGuid();

    public NotificationsRlsTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _appConnection = await _fixture.AppUserDataSource!.OpenConnectionAsync();

        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();
        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = $@"
            INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at, is_active)
            VALUES
                ('{_companyA}', 'Company A LLC', 'Company A', '+962790000001', 'individual_owner'::company_type_enum, now(), now(), true),
                ('{_companyB}', 'Company B LLC', 'Company B', '+962790000002', 'individual_owner'::company_type_enum, now(), now(), true);

            INSERT INTO users (id, full_name, email)
            VALUES
                ('{_userA1}', 'User A1', 'a1@example.com'),
                ('{_userA2}', 'User A2', 'a2@example.com');

            INSERT INTO notifications (id, company_id, recipient_user_id, notification_type, subject, body)
            VALUES
                ('{Guid.NewGuid()}', '{_companyA}', '{_userA1}', 'general_notification'::notification_type_enum, 'For A1', 'Body A1'),
                ('{Guid.NewGuid()}', '{_companyA}', '{_userA2}', 'general_notification'::notification_type_enum, 'For A2', 'Body A2');

            INSERT INTO notification_templates (id, company_id, template_name, notification_type, subject, body)
            VALUES ('{Guid.NewGuid()}', '{_companyA}', 'Welcome', 'general_notification'::notification_type_enum, 'Hi', 'Hello');
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_appConnection != null)
            await _appConnection.DisposeAsync();
    }

    private async Task<int> CountAsync(string sql, Guid companyId, Guid? userId)
    {
        await using var tx = await _appConnection!.BeginTransactionAsync();
        await using var setCmd = _appConnection.CreateCommand();
        setCmd.CommandText = $"SET LOCAL app.current_company_id = '{companyId}';";
        if (userId.HasValue)
            setCmd.CommandText += $" SET LOCAL app.current_user_id = '{userId}';";
        setCmd.Transaction = tx;
        await setCmd.ExecuteNonQueryAsync();

        await using var queryCmd = _appConnection.CreateCommand();
        queryCmd.CommandText = sql;
        queryCmd.Transaction = tx;
        var count = Convert.ToInt32(await queryCmd.ExecuteScalarAsync());
        await tx.RollbackAsync();
        return count;
    }

    [Fact]
    public async Task Rls_Notifications_RecipientOwnership_IsEnforcedPerRow()
    {
        // Each user sees only their own inbox within the same company.
        (await CountAsync("SELECT COUNT(*) FROM notifications;", _companyA, _userA1)).Should().Be(1);
        (await CountAsync("SELECT COUNT(*) FROM notifications;", _companyA, _userA2)).Should().Be(1);
    }

    [Fact]
    public async Task Rls_Notifications_SystemContext_SeesWholeCompany()
    {
        // No app.current_user_id set = trusted system context (background dispatch jobs).
        (await CountAsync("SELECT COUNT(*) FROM notifications;", _companyA, null)).Should().Be(2);
    }

    [Fact]
    public async Task Rls_Notifications_CrossTenant_IsBlocked()
    {
        (await CountAsync("SELECT COUNT(*) FROM notifications;", _companyB, null)).Should().Be(0);
        (await CountAsync("SELECT COUNT(*) FROM notification_templates;", _companyB, null)).Should().Be(0);
    }

    [Fact]
    public async Task Rls_NotificationTemplates_TenantIsolation_Works()
    {
        (await CountAsync("SELECT COUNT(*) FROM notification_templates;", _companyA, _userA1)).Should().Be(1);
    }
}
