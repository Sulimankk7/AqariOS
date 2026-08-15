using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class ProvisionTenantAccountCommandHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Test Fakes (nested private to avoid CS9051 with file-local types)
    // ─────────────────────────────────────────────────────────────────────────

    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; init; }
        public bool IsPlatformAdmin { get; init; }
    }

    private class FakeEmailSender : IEmailSender
    {
        public bool ShouldSucceed { get; set; } = true;
        public int SendCount { get; private set; }

        public Task<bool> SendTenantActivationEmailAsync(
            string recipientEmail, string tenantName, string activationToken,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult(ShouldSucceed);
        }
    }

    private class FakeSmsSender : ISmsSender
    {
        public bool ShouldSucceed { get; set; } = true;
        public int SendCount { get; private set; }
        public string? LastRecipientPhone { get; private set; }
        public string? LastTenantName { get; private set; }
        public string? LastActivationToken { get; private set; }

        public Task<bool> SendTenantActivationSmsAsync(
            string recipientPhone, string tenantName, string activationToken,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            LastRecipientPhone = recipientPhone;
            LastTenantName = tenantName;
            LastActivationToken = activationToken;
            return Task.FromResult(ShouldSucceed);
        }
    }

    private class FakePostCommitRegistrar : IPostCommitRegistrar
    {
        private readonly List<Func<CancellationToken, Task>> _actions = new();
        public IReadOnlyList<Func<CancellationToken, Task>> Actions => _actions.AsReadOnly();

        public void RegisterPostCommitAction(Func<CancellationToken, Task> action) => _actions.Add(action);
        public void Clear() => _actions.Clear();

        public async Task RunAllAsync(CancellationToken ct = default)
        {
            var snapshot = new List<Func<CancellationToken, Task>>(_actions);
            _actions.Clear();
            foreach (var a in snapshot) await a(ct);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PropertyOsDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PropertyOsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private static Tenant MakeTenant(Guid companyId, string phone = "+962791112223") =>
        Tenant.Create(companyId, "Test Tenant", "123456789", phone, DateTimeOffset.UtcNow, null);

    private static User MakeUser(string phone, string? email = null, string? passwordHash = null) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            FullName = "Existing User",
            Phone = phone,
            Email = email,
            PasswordHash = passwordHash,
            PasswordAlgorithm = "argon2id",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    private static (ProvisionTenantAccountCommandHandler handler, FakeEmailSender email, FakeSmsSender sms, FakePostCommitRegistrar postCommit)
        BuildHandler(ITenantRepository repo, ITenantContext ctx, PropertyOsDbContext db)
    {
        var email = new FakeEmailSender();
        var sms = new FakeSmsSender();
        var postCommit = new FakePostCommitRegistrar();
        var handler = new ProvisionTenantAccountCommandHandler(
            repo, ctx, db, email, sms, postCommit,
            NullLogger<ProvisionTenantAccountCommandHandler>.Instance);
        return (handler, email, sms, postCommit);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PATH A — Tenant already has a linked User
    // ═════════════════════════════════════════════════════════════════════════

    // Case 1 — PATH A: delivery email = linked user's own email -> succeeds.
    [Fact]
    public async Task PathA_DeliveryEmail_IsLinkedUsersOwnEmail_Succeeds()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var pendingUser = MakeUser("+962791112223", email: "omar@example.com", passwordHash: null);
        db.Users.Add(pendingUser);
        var tenant = MakeTenant(companyId);
        tenant.LinkUser(pendingUser.Id);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, _, _, postCommit) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(tenant.Id, TenantProvisioningContactMethod.Email, Email: "omar@example.com"),
            CancellationToken.None);

        result.UserId.Should().Be(pendingUser.Id);
        result.ActivationToken.Should().NotBeNullOrWhiteSpace();
        postCommit.Actions.Should().HaveCount(1, "pending account must register an email action");
    }

    // Case 2 — PATH A: delivery email = a different unused email -> succeeds.
    [Fact]
    public async Task PathA_DeliveryEmail_IsUnusedEmail_Succeeds()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var pendingUser = MakeUser("+962791112223", email: "omar@example.com", passwordHash: null);
        db.Users.Add(pendingUser);
        var tenant = MakeTenant(companyId);
        tenant.LinkUser(pendingUser.Id);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, _, _, postCommit) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(tenant.Id, TenantProvisioningContactMethod.Email, Email: "alternative@example.com"),
            CancellationToken.None);

        result.UserId.Should().Be(pendingUser.Id);
        result.ActivationToken.Should().NotBeNullOrWhiteSpace();
        postCommit.Actions.Should().HaveCount(1, "pending account must register an email action");
    }

    // Case 3 — PATH A: delivery email belongs to another tenant's User -> 409 EMAIL_BELONGS_TO_ANOTHER_TENANT.
    [Fact]
    public async Task PathA_DeliveryEmail_BelongsToAnotherTenantsUser_Throws409EmailConflict()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var linkedUser = MakeUser("+962791112223", email: "omar@example.com", passwordHash: null);
        db.Users.Add(linkedUser);
        var tenantA = MakeTenant(companyId, phone: "+962791112223");
        tenantA.LinkUser(linkedUser.Id);
        db.Tenants.Add(tenantA);

        var otherUser = MakeUser("+962799999999", email: "ahmad@example.com");
        db.Users.Add(otherUser);
        var tenantB = MakeTenant(companyId, phone: "+962799999999");
        tenantB.LinkUser(otherUser.Id);
        db.Tenants.Add(tenantB);

        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenantA, tenantB } };
        var (handler, _, _, _) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        Func<Task> act = () => handler.Handle(
            new ProvisionTenantAccountCommand(tenantA.Id, TenantProvisioningContactMethod.Email, Email: "ahmad@example.com"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*EMAIL_BELONGS_TO_ANOTHER_TENANT*");
    }

    // Case 1 (active reactivation): active account receives a fresh activation/reset token and registers email.
    [Fact]
    public async Task PathA_ActiveLinkedUser_EmailMode_IssuesFreshTokenAndRegistersEmail()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var activeUser = MakeUser("+962791112223", email: "active@example.com", passwordHash: "bcrypt_hash");
        db.Users.Add(activeUser);
        var tenant = MakeTenant(companyId);
        tenant.LinkUser(activeUser.Id);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, _, _, postCommit) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(tenant.Id, TenantProvisioningContactMethod.Email, Email: "active@example.com"),
            CancellationToken.None);

        result.ActivationToken.Should().NotBeNullOrWhiteSpace("active account must receive a fresh activation link upon request");
        postCommit.Actions.Should().HaveCount(1, "email action must be registered post-commit");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PATH B — Tenant.UserId is null; channel-scoped conflict and lookup
    // ═════════════════════════════════════════════════════════════════════════

    // Case 4 — THE RUNTIME BUG FIX:
    // Email mode, Tenant.Phone resolves to User A (unlinked),
    // entered email resolves to User B (unlinked) -> SUCCEEDS without identity-split error.
    [Fact]
    public async Task PathB_EmailMode_PhoneResolvesToDifferentUnlinkedUser_EmailUserSafe_Succeeds()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var userA = MakeUser("+962786630009", passwordHash: null);
        db.Users.Add(userA);

        var userB = MakeUser("+962799000001", email: "portal@example.com", passwordHash: null);
        db.Users.Add(userB);

        var tenant = MakeTenant(companyId, phone: "+962786630009");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, _, _, postCommit) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(
                tenant.Id,
                ContactMethod: TenantProvisioningContactMethod.Email,
                Email: "portal@example.com"),
            CancellationToken.None);

        result.UserId.Should().Be(userB.Id, "handler must reuse the user found by the entered email");
        tenant.UserId.Should().Be(userB.Id);
        result.ActivationToken.Should().NotBeNullOrWhiteSpace();
        postCommit.Actions.Should().HaveCount(1, "activation email must be registered post-commit");
    }

    // Case 5 — Email mode, entered email belongs to another tenant -> 409 EMAIL_BELONGS_TO_ANOTHER_TENANT.
    [Fact]
    public async Task PathB_EmailMode_EmailBelongsToAnotherTenant_Throws409EmailConflict()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var otherUser = MakeUser("+962799000001", email: "omar@example.com");
        db.Users.Add(otherUser);
        var otherTenant = MakeTenant(companyId, phone: "+962799000001");
        otherTenant.LinkUser(otherUser.Id);
        db.Tenants.Add(otherTenant);

        var tenant = MakeTenant(companyId, phone: "+962791112223");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant, otherTenant } };
        var (handler, _, _, _) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        Func<Task> act = () => handler.Handle(
            new ProvisionTenantAccountCommand(
                tenant.Id,
                ContactMethod: TenantProvisioningContactMethod.Email,
                Email: "omar@example.com"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*EMAIL_BELONGS_TO_ANOTHER_TENANT*");

        tenant.UserId.Should().BeNull("must not link when email belongs to another tenant");
    }

    // Case 6 — Phone mode, email resolves to an unrelated User -> must NOT cause rejection.
    [Fact]
    public async Task PathB_PhoneMode_EmailResolvesToUnrelatedUser_NoIdentitySplitError_Succeeds()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var userA = MakeUser("+962791112223", passwordHash: null);
        db.Users.Add(userA);

        var userB = MakeUser("+962799000001", email: "unrelated@example.com", passwordHash: null);
        db.Users.Add(userB);

        var tenant = MakeTenant(companyId, phone: "+962791112223");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, _, _, _) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(
                tenant.Id,
                ContactMethod: TenantProvisioningContactMethod.Phone,
                Phone: "+962791112223",
                Email: "unrelated@example.com"),
            CancellationToken.None);

        result.UserId.Should().Be(userA.Id, "handler must reuse the user found by the selected phone");
        tenant.UserId.Should().Be(userA.Id);
    }

    // Case 7 — Phone mode, phone belongs to another tenant -> 409 PHONE_BELONGS_TO_ANOTHER_TENANT.
    [Fact]
    public async Task PathB_PhoneMode_PhoneBelongsToAnotherTenant_Throws409PhoneConflict()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var phoneUser = MakeUser("+962791112223");
        db.Users.Add(phoneUser);

        var otherTenant = MakeTenant(companyId, phone: "+962791112223");
        otherTenant.LinkUser(phoneUser.Id);
        db.Tenants.Add(otherTenant);

        var tenant = MakeTenant(companyId, phone: "+962791112223");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant, otherTenant } };
        var (handler, _, _, _) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        Func<Task> act = () => handler.Handle(
            new ProvisionTenantAccountCommand(tenant.Id, Phone: "+962791112223"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*PHONE_BELONGS_TO_ANOTHER_TENANT*");

        tenant.UserId.Should().BeNull("must not link when phone belongs to another tenant");
    }

    // Case 8 — User already linked to another Tenant in the SAME company is never reused.
    [Fact]
    public async Task PathB_UserLinkedToAnotherTenantInSameCompany_IsNeverReused()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var sharedUser = MakeUser("+962799000001", email: "shared@example.com");
        db.Users.Add(sharedUser);
        var tenantB = MakeTenant(companyId, phone: "+962799000001");
        tenantB.LinkUser(sharedUser.Id);
        db.Tenants.Add(tenantB);

        var tenantA = MakeTenant(companyId, phone: "+962791112223");
        db.Tenants.Add(tenantA);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenantA, tenantB } };
        var (handler, _, _, _) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        // Email mode
        Func<Task> emailAct = () => handler.Handle(
            new ProvisionTenantAccountCommand(
                tenantA.Id,
                ContactMethod: TenantProvisioningContactMethod.Email,
                Email: "shared@example.com"),
            CancellationToken.None);

        await emailAct.Should().ThrowAsync<ConflictException>()
            .WithMessage("*EMAIL_BELONGS_TO_ANOTHER_TENANT*");

        // Phone mode
        Func<Task> phoneAct = () => handler.Handle(
            new ProvisionTenantAccountCommand(
                tenantA.Id,
                ContactMethod: TenantProvisioningContactMethod.Phone,
                Phone: "+962799000001"),
            CancellationToken.None);

        await phoneAct.Should().ThrowAsync<ConflictException>()
            .WithMessage("*PHONE_BELONGS_TO_ANOTHER_TENANT*");

        tenantA.UserId.Should().BeNull("tenantA must remain unmodified after both conflict rejections");
    }

    // Case 9 — Existing unlinked User is not duplicated when selected contact matches.
    [Fact]
    public async Task PathB_EmailMode_ExistingUnlinkedUserReused_NoDuplicateUserCreated()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var existingUser = MakeUser("+962799000001", email: "existing@example.com", passwordHash: null);
        db.Users.Add(existingUser);
        var tenant = MakeTenant(companyId, phone: "+962791112223");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, _, _, _) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(
                tenant.Id,
                ContactMethod: TenantProvisioningContactMethod.Email,
                Email: "existing@example.com"),
            CancellationToken.None);

        result.UserId.Should().Be(existingUser.Id, "must reuse the unlinked user matching the selected email");
        tenant.UserId.Should().Be(existingUser.Id);
        (await db.Users.CountAsync()).Should().Be(1, "must NOT create a duplicate user");
    }

    // Case 10 — Activation email is registered post-commit, not inline.
    [Fact]
    public async Task PathB_EmailMode_BrevoFailsAfterCommit_AccountCommitted_EmailSentFalse()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var tenant = MakeTenant(companyId, phone: "+962791112223");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var emailSender = new FakeEmailSender { ShouldSucceed = false };
        var smsSender = new FakeSmsSender();
        var postCommit = new FakePostCommitRegistrar();
        var handler = new ProvisionTenantAccountCommandHandler(
            repo, new FakeTenantContext { CompanyId = companyId }, db,
            emailSender, smsSender, postCommit,
            NullLogger<ProvisionTenantAccountCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(
                tenant.Id,
                ContactMethod: TenantProvisioningContactMethod.Email,
                Email: "tenant@example.com"),
            CancellationToken.None);

        await postCommit.RunAllAsync();

        result.EmailSent.Should().BeFalse("email failure must not propagate to a thrown exception");
        (await db.Users.CountAsync()).Should().Be(1, "account must remain committed even when email fails");
        tenant.UserId.Should().NotBeNull("tenant must remain linked even when email fails");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Additional invariants
    // ═════════════════════════════════════════════════════════════════════════

    // Cross-company reuse: User linked to tenant in different company allowed.
    [Fact]
    public async Task PathB_PhoneMode_UserLinkedToTenantInDifferentCompany_AllowedAsReuse()
    {
        using var db = CreateDb();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        var user = MakeUser("+962791112223", passwordHash: null);
        db.Users.Add(user);
        var tenantInCompanyB = MakeTenant(companyB, phone: "+962791112223");
        tenantInCompanyB.LinkUser(user.Id);
        db.Tenants.Add(tenantInCompanyB);

        var tenantInCompanyA = MakeTenant(companyA, phone: "+962791112223");
        db.Tenants.Add(tenantInCompanyA);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenantInCompanyA } };
        var (handler, _, _, _) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyA }, db);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(tenantInCompanyA.Id, Phone: "+962791112223"),
            CancellationToken.None);

        result.UserId.Should().Be(user.Id, "user linked to a different company's tenant is not a conflict");
        tenantInCompanyA.UserId.Should().Be(user.Id);
    }

    // No existing user -> creates exactly one new User.
    [Fact]
    public async Task PathB_NoExistingUser_CreatesExactlyOneUser_LinksToTenant()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var tenant = MakeTenant(companyId, phone: "+962791112223");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, _, _, _) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(tenant.Id, Phone: "+962791112223"),
            CancellationToken.None);

        result.UserId.Should().NotBeEmpty();
        result.ActivationToken.Should().NotBeNullOrWhiteSpace();
        tenant.UserId.Should().Be(result.UserId);
        (await db.Users.CountAsync()).Should().Be(1);
    }

    // Cross-company access attempt returns 404, no mutation.
    [Fact]
    public async Task Handle_TenantBelongsToDifferentCompany_ThrowsNotFoundException()
    {
        using var db = CreateDb();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        var tenant = MakeTenant(companyA);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, _, _, _) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyB }, db);

        Func<Task> act = () => handler.Handle(new ProvisionTenantAccountCommand(tenant.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        tenant.UserId.Should().BeNull("no data must have been mutated");
    }

    // Reusing an existing user ensures the TENANT company role is created.
    [Fact]
    public async Task PathB_PhoneMode_ExistingUserReused_EnsuresTenantRoleInCompany()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var existingUser = MakeUser("+962791112223", passwordHash: null);
        db.Users.Add(existingUser);
        var tenant = MakeTenant(companyId, phone: "+962791112223");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, _, _, _) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        await handler.Handle(
            new ProvisionTenantAccountCommand(tenant.Id, Phone: "+962791112223"),
            CancellationToken.None);

        var roleEntry = await db.UserCompanyRoles
            .FirstOrDefaultAsync(ucr => ucr.UserId == existingUser.Id && ucr.CompanyId == companyId);
        roleEntry.Should().NotBeNull("TENANT company role must be created when reusing an existing user");
        tenant.UserId.Should().Be(existingUser.Id);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Twilio SMS Delivery Tests
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PathB_PhoneMode_RegistersSmsPostCommitAction_AndSendsSmsAfterCommit()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var tenant = MakeTenant(companyId, phone: "+962791112223");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, email, sms, postCommit) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(
                tenant.Id,
                ContactMethod: TenantProvisioningContactMethod.Phone,
                Phone: "+962791112223"),
            CancellationToken.None);

        // Before commit execution
        sms.SendCount.Should().Be(0, "SMS must not be dispatched before transaction commit");
        email.SendCount.Should().Be(0, "Email must not be dispatched when Phone mode is selected");
        postCommit.Actions.Should().HaveCount(1, "SMS action must be registered post-commit");

        // Execute post-commit
        await postCommit.RunAllAsync();

        sms.SendCount.Should().Be(1, "SMS must be dispatched exactly once post-commit");
        sms.LastRecipientPhone.Should().Be("+962791112223");
        sms.LastTenantName.Should().Be("Test Tenant");
        sms.LastActivationToken.Should().Be(result.ActivationToken);
        result.SmsSent.Should().BeTrue();
        result.EmailSent.Should().BeFalse();
    }

    [Fact]
    public async Task PathB_PhoneMode_SmsFailure_DoesNotRollbackTenantAccount()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var tenant = MakeTenant(companyId, phone: "+962791112223");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var emailSender = new FakeEmailSender();
        var smsSender = new FakeSmsSender { ShouldSucceed = false };
        var postCommit = new FakePostCommitRegistrar();
        var handler = new ProvisionTenantAccountCommandHandler(
            repo, new FakeTenantContext { CompanyId = companyId }, db,
            emailSender, smsSender, postCommit,
            NullLogger<ProvisionTenantAccountCommandHandler>.Instance);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(
                tenant.Id,
                ContactMethod: TenantProvisioningContactMethod.Phone,
                Phone: "+962791112223"),
            CancellationToken.None);

        await postCommit.RunAllAsync();

        result.SmsSent.Should().BeFalse("SMS failure must result in SmsSent = false");
        (await db.Users.CountAsync()).Should().Be(1, "account must remain committed even when SMS dispatch fails");
        tenant.UserId.Should().NotBeNull("tenant must remain linked even when SMS dispatch fails");
        result.ActivationToken.Should().NotBeNullOrWhiteSpace("manual activation token fallback must be preserved");
    }

    [Fact]
    public async Task PathA_PhoneMode_AlreadyLinkedTenant_RegistersSmsPostCommitAction()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();

        var linkedUser = MakeUser("+962791112223", passwordHash: null);
        db.Users.Add(linkedUser);
        var tenant = MakeTenant(companyId, phone: "+962791112223");
        tenant.LinkUser(linkedUser.Id);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var (handler, email, sms, postCommit) = BuildHandler(repo, new FakeTenantContext { CompanyId = companyId }, db);

        var result = await handler.Handle(
            new ProvisionTenantAccountCommand(
                tenant.Id,
                ContactMethod: TenantProvisioningContactMethod.Phone,
                Phone: "+962791112223"),
            CancellationToken.None);

        result.UserId.Should().Be(linkedUser.Id);
        postCommit.Actions.Should().HaveCount(1, "SMS action must be registered post-commit for linked tenant");

        await postCommit.RunAllAsync();

        sms.SendCount.Should().Be(1);
        sms.LastRecipientPhone.Should().Be("+962791112223");
        result.SmsSent.Should().BeTrue();
    }
}
