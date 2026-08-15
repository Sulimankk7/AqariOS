using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;

/// <summary>
/// Enables Tenant Portal access for an existing Tenant person record.
///
/// Business rules:
///   1. Tenant.UserId != null (PATH A):
///      - Reuses the exact linked User identity.
///      - In Email mode, the entered email is validated only to ensure it does not belong
///        to a User linked to ANOTHER Tenant in the SAME company (EMAIL_BELONGS_TO_ANOTHER_TENANT).
///      - Never replaces Tenant.UserId; never performs cross-channel split checks.
///
///   2. Tenant.UserId == null (PATH B):
///      - Email mode: lookup and conflict checks are scoped to the entered email.
///        If email's User belongs to another Tenant in same company -> 409 EMAIL_BELONGS_TO_ANOTHER_TENANT.
///        Otherwise reuses unlinked User or creates ONE new User.
///      - Phone mode: lookup and conflict checks are scoped to the selected phone.
///        If phone's User belongs to another Tenant in same company -> 409 PHONE_BELONGS_TO_ANOTHER_TENANT.
///        Otherwise reuses unlinked User or creates ONE new User.
///      - The non-selected contact channel is NEVER used to reject the provisioning flow.
///      - No IDENTITY_CONTACTS_REFERENCE_DIFFERENT_USERS check in normal flow.
/// </summary>
public class ProvisionTenantAccountCommandHandler
    : IRequestHandler<ProvisionTenantAccountCommand, ProvisionTenantAccountResponseDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly IPostCommitRegistrar _postCommitRegistrar;
    private readonly ILogger<ProvisionTenantAccountCommandHandler> _logger;

    public ProvisionTenantAccountCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IApplicationDbContext dbContext,
        IEmailSender emailSender,
        ISmsSender smsSender,
        IPostCommitRegistrar postCommitRegistrar,
        ILogger<ProvisionTenantAccountCommandHandler> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));
        _postCommitRegistrar = postCommitRegistrar ?? throw new ArgumentNullException(nameof(postCommitRegistrar));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProvisionTenantAccountResponseDto> Handle(
        ProvisionTenantAccountCommand request,
        CancellationToken cancellationToken)
    {
        // ── CompanyId is always derived from authenticated JWT claims, never from caller ──
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Authenticated company context is required.");

        // ── Load tenant scoped to both TenantId AND CompanyId ──────────────────────────
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null || tenant.CompanyId != companyId)
            throw new NotFoundException($"Tenant {request.TenantId} was not found.");

        _logger.LogInformation(
            "Tenant portal provisioning initiated. TenantId={TenantId} CompanyId={CompanyId}",
            tenant.Id, companyId);

        // ══════════════════════════════════════════════════════════════════════════════════
        // PATH A — Tenant.UserId is already set.
        // Load THAT user only. Never search another. Never create another user.
        // ══════════════════════════════════════════════════════════════════════════════════
        if (tenant.UserId.HasValue)
        {
            return await HandleAlreadyLinkedTenantAsync(tenant, companyId, request, cancellationToken);
        }

        // ══════════════════════════════════════════════════════════════════════════════════
        // PATH B — Tenant.UserId is null.
        //
        // The Tenant record is the authoritative business aggregate. This operation enables
        // portal access for THIS Tenant — it is NOT an identity-verification workflow.
        //
        // ContactMethod controls TWO things:
        //   1. Where the activation link is DELIVERED (email vs. phone/SMS).
        //   2. Which contact is used to look up or create the portal User identity.
        //
        // Conflict protection is channel-scoped:
        //   Email mode → only reject if the ENTERED EMAIL belongs to another tenant.
        //   Phone mode → only reject if the SELECTED PHONE belongs to another tenant.
        //
        // The selected channel's User is resolved first. The other channel is NOT used
        // to reject a valid provisioning request and does NOT trigger identity-split errors.
        //
        // Cross-tenant protection: a User already linked to a DIFFERENT Tenant in the SAME
        // company is never silently attached to the current Tenant.
        // ══════════════════════════════════════════════════════════════════════════════════

        var effectivePhone = NormalizePhone(request.Phone) ?? NormalizePhone(tenant.Phone);
        var effectiveEmail = NormalizeEmail(request.Email);

        if (request.ContactMethod == TenantProvisioningContactMethod.Email)
        {
            // ── Email mode ───────────────────────────────────────────────────────────────
            // Conflict check and portal User lookup are scoped to the entered email.
            // Tenant.Phone resolving to a different User is irrelevant here and does NOT
            // cause an IDENTITY_CONTACTS_REFERENCE_DIFFERENT_USERS error.

            var userByEmail = !string.IsNullOrEmpty(effectiveEmail)
                ? await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == effectiveEmail && u.DeletedAt == null, cancellationToken)
                : null;

            if (userByEmail != null)
            {
                // Check: is this email's User linked to a DIFFERENT Tenant in this company?
                var emailUserOtherTenantId = await _dbContext.Tenants
                    .Where(t => t.CompanyId == companyId
                             && t.UserId == userByEmail.Id
                             && t.Id != tenant.Id
                             && t.DeletedAt == null)
                    .Select(t => (Guid?)t.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (emailUserOtherTenantId.HasValue)
                {
                    _logger.LogWarning(
                        "Email contact resolves to User {UserId} linked to Tenant {OtherTenantId}. TenantId={TenantId}",
                        userByEmail.Id, emailUserOtherTenantId.Value, tenant.Id);
                    throw new ConflictException(
                        "EMAIL_BELONGS_TO_ANOTHER_TENANT: " +
                        "The provided email address is already associated with another tenant portal account in this company.");
                }

                // Email's User is safe to reuse — not owned by another tenant.
                _logger.LogInformation(
                    "Reusing existing User {UserId} (found by email) for TenantId={TenantId}.",
                    userByEmail.Id, tenant.Id);
                return await HandleLinkExistingUserAsync(
                    tenant, companyId, userByEmail, request, cancellationToken);
            }

            // No User with this email → create a new portal User.
            // Race-condition constraint violations (uq_users_phone / uq_users_email) are
            // caught by GlobalExceptionHandler and mapped to HTTP 409.
            _logger.LogInformation(
                "No existing User found by email. Creating new portal identity for TenantId={TenantId}.", tenant.Id);
            return await HandleCreateNewUserAsync(
                tenant, companyId, effectivePhone!, effectiveEmail, request, cancellationToken);
        }
        else
        {
            // ── Phone mode ───────────────────────────────────────────────────────────────
            // Conflict check and portal User lookup are scoped to the selected phone.
            // Email resolving to a different User is irrelevant here and does NOT
            // cause an IDENTITY_CONTACTS_REFERENCE_DIFFERENT_USERS error.

            var userByPhone = !string.IsNullOrEmpty(effectivePhone)
                ? await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Phone == effectivePhone && u.DeletedAt == null, cancellationToken)
                : null;

            if (userByPhone != null)
            {
                // Check: is this phone's User linked to a DIFFERENT Tenant in this company?
                var phoneUserOtherTenantId = await _dbContext.Tenants
                    .Where(t => t.CompanyId == companyId
                             && t.UserId == userByPhone.Id
                             && t.Id != tenant.Id
                             && t.DeletedAt == null)
                    .Select(t => (Guid?)t.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (phoneUserOtherTenantId.HasValue)
                {
                    _logger.LogWarning(
                        "Phone contact resolves to User {UserId} linked to Tenant {OtherTenantId}. TenantId={TenantId}",
                        userByPhone.Id, phoneUserOtherTenantId.Value, tenant.Id);
                    throw new ConflictException(
                        "PHONE_BELONGS_TO_ANOTHER_TENANT: " +
                        "The provided phone number is already associated with another tenant portal account in this company.");
                }

                // Phone's User is safe to reuse — not owned by another tenant.
                _logger.LogInformation(
                    "Reusing existing User {UserId} (found by phone) for TenantId={TenantId}.",
                    userByPhone.Id, tenant.Id);
                return await HandleLinkExistingUserAsync(
                    tenant, companyId, userByPhone, request, cancellationToken);
            }

            // No User with this phone → create a new portal User.
            _logger.LogInformation(
                "No existing User found by phone. Creating new portal identity for TenantId={TenantId}.", tenant.Id);
            return await HandleCreateNewUserAsync(
                tenant, companyId, effectivePhone!, effectiveEmail, request, cancellationToken);
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════════
    // PATH A: Tenant.UserId is already set
    // ════════════════════════════════════════════════════════════════════════════════════
    private async Task<ProvisionTenantAccountResponseDto> HandleAlreadyLinkedTenantAsync(
        Domain.Leasing.Tenant tenant,
        Guid companyId,
        ProvisionTenantAccountCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Tenant {TenantId} already linked to User {UserId}. Loading exact linked identity.",
            tenant.Id, tenant.UserId!.Value);

        var linkedUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == tenant.UserId.Value && u.DeletedAt == null, cancellationToken);

        if (linkedUser == null)
            throw new InvalidOperationException(
                $"Linked User {tenant.UserId.Value} was not found. Data integrity issue.");

        // ── Validate delivery email ownership ───────────────────────────────────────
        // If a specific email was provided and it belongs to a User who is linked to a
        // different Tenant in this company, reject it. If the email is this tenant's own
        // linked user's email, or belongs to an unlinked User, or does not exist, allow it.
        var requestedEmail = NormalizeEmail(request.Email);
        if (!string.IsNullOrEmpty(requestedEmail) && requestedEmail != NormalizeEmail(linkedUser.Email))
        {
            var emailOwner = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == requestedEmail && u.DeletedAt == null, cancellationToken);

            if (emailOwner != null && emailOwner.Id != linkedUser.Id)
            {
                var isEmailOwnerLinkedToAnotherTenant = await _dbContext.Tenants
                    .AnyAsync(t => t.CompanyId == companyId
                                && t.UserId == emailOwner.Id
                                && t.Id != tenant.Id
                                && t.DeletedAt == null,
                              cancellationToken);

                if (isEmailOwnerLinkedToAnotherTenant)
                {
                    _logger.LogWarning(
                        "PATH A: Delivery email belongs to User {UserId} linked to a different Tenant. TenantId={TenantId}",
                        emailOwner.Id, tenant.Id);
                    throw new ConflictException(
                        "EMAIL_BELONGS_TO_ANOTHER_TENANT: " +
                        "The provided email address is already associated with another tenant portal account in this company.");
                }
            }
        }

        var tenantRole = await GetOrCreateTenantRoleAsync(cancellationToken);
        await EnsureUserCompanyRoleAsync(linkedUser.Id, companyId, tenantRole.Id, cancellationToken);

        // Always issue a fresh activation/reset token for this provisioning request
        var rawToken = GenerateRawToken();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(48);

        linkedUser.PasswordResetTokenHash = HashToken(rawToken);
        linkedUser.PasswordResetExpiresAt = expiresAt;
        linkedUser.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[DIAG:ProvisionTenantAccount] Activation token generated for linked User {UserId}. TenantId={TenantId}",
            linkedUser.Id, tenant.Id);

        var dto = BuildResponseDto(tenant.Id, linkedUser.Id, companyId, rawToken, expiresAt);

        if (request.ContactMethod == TenantProvisioningContactMethod.Email)
        {
            var deliveryEmail = requestedEmail ?? NormalizeEmail(linkedUser.Email);
            if (!string.IsNullOrEmpty(deliveryEmail) && rawToken != null)
            {
                RegisterEmailPostCommitAction(dto, deliveryEmail, tenant.Name, tenant.Id, rawToken);
            }
        }
        else if (request.ContactMethod == TenantProvisioningContactMethod.Phone)
        {
            var deliveryPhone = NormalizePhone(request.Phone) ?? NormalizePhone(linkedUser.Phone) ?? NormalizePhone(tenant.Phone);
            if (!string.IsNullOrEmpty(deliveryPhone) && rawToken != null)
            {
                RegisterSmsPostCommitAction(dto, deliveryPhone, tenant.Name, tenant.Id, rawToken);
            }
        }

        return dto;
    }

    // ════════════════════════════════════════════════════════════════════════════════════
    // PATH B1: No existing user found — create exactly one new User
    // ════════════════════════════════════════════════════════════════════════════════════
    private async Task<ProvisionTenantAccountResponseDto> HandleCreateNewUserAsync(
        Domain.Leasing.Tenant tenant,
        Guid companyId,
        string effectivePhone,
        string? effectiveEmail,
        ProvisionTenantAccountCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "No existing User found. Creating new portal identity for TenantId={TenantId}.", tenant.Id);

        var rawToken = GenerateRawToken();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(48);

        var newUser = new User
        {
            Id = Guid.CreateVersion7(),
            FullName = tenant.Name,
            Phone = effectivePhone,
            Email = effectiveEmail,
            PasswordHash = null,
            PasswordAlgorithm = "argon2id",
            PreferredLanguage = "ar",
            IsActive = true,
            PasswordResetTokenHash = HashToken(rawToken),
            PasswordResetExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Users.Add(newUser);

        var tenantRole = await GetOrCreateTenantRoleAsync(cancellationToken);
        _dbContext.UserCompanyRoles.Add(BuildMembership(newUser.Id, companyId, tenantRole.Id));

        tenant.LinkUser(newUser.Id);

        // Constraint violations (uq_users_phone / uq_users_email) from a race condition
        // are already caught by TransactionBehavior and mapped to HTTP 409 by
        // GlobalExceptionHandler (case DbUpdateException / PostgresException 23505).
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[DIAG:ProvisionTenantAccount] Activation token generated for new User {UserId}. TenantId={TenantId}",
            newUser.Id, tenant.Id);

        var dto = BuildResponseDto(tenant.Id, newUser.Id, companyId, rawToken, expiresAt);

        if (request.ContactMethod == TenantProvisioningContactMethod.Email)
        {
            var deliveryEmail = NormalizeEmail(request.Email) ?? effectiveEmail;
            if (!string.IsNullOrEmpty(deliveryEmail) && rawToken != null)
            {
                RegisterEmailPostCommitAction(dto, deliveryEmail, tenant.Name, tenant.Id, rawToken);
            }
        }
        else if (request.ContactMethod == TenantProvisioningContactMethod.Phone)
        {
            var deliveryPhone = NormalizePhone(request.Phone) ?? effectivePhone;
            if (!string.IsNullOrEmpty(deliveryPhone) && rawToken != null)
            {
                RegisterSmsPostCommitAction(dto, deliveryPhone, tenant.Name, tenant.Id, rawToken);
            }
        }

        return dto;
    }

    // ════════════════════════════════════════════════════════════════════════════════════
    // PATH B2: Existing User found — reuse it for this tenant's portal access
    // ════════════════════════════════════════════════════════════════════════════════════
    private async Task<ProvisionTenantAccountResponseDto> HandleLinkExistingUserAsync(
        Domain.Leasing.Tenant tenant,
        Guid companyId,
        User existingUser,
        ProvisionTenantAccountCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reusing existing User {UserId} as portal identity for TenantId={TenantId}.",
            existingUser.Id, tenant.Id);

        var tenantRole = await GetOrCreateTenantRoleAsync(cancellationToken);
        await EnsureUserCompanyRoleAsync(existingUser.Id, companyId, tenantRole.Id, cancellationToken);

        tenant.LinkUser(existingUser.Id);

        // Always issue a fresh activation/reset token for this provisioning request
        var rawToken = GenerateRawToken();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(48);

        existingUser.PasswordResetTokenHash = HashToken(rawToken);
        existingUser.PasswordResetExpiresAt = expiresAt;
        existingUser.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[DIAG:ProvisionTenantAccount] Activation token generated for existing User {UserId}. TenantId={TenantId}",
            existingUser.Id, tenant.Id);

        var dto = BuildResponseDto(tenant.Id, existingUser.Id, companyId, rawToken, expiresAt);

        if (request.ContactMethod == TenantProvisioningContactMethod.Email)
        {
            var deliveryEmail = NormalizeEmail(request.Email) ?? NormalizeEmail(existingUser.Email);
            if (!string.IsNullOrEmpty(deliveryEmail) && rawToken != null)
            {
                RegisterEmailPostCommitAction(dto, deliveryEmail, tenant.Name, tenant.Id, rawToken);
            }
        }
        else if (request.ContactMethod == TenantProvisioningContactMethod.Phone)
        {
            var deliveryPhone = NormalizePhone(request.Phone) ?? NormalizePhone(existingUser.Phone) ?? NormalizePhone(tenant.Phone);
            if (!string.IsNullOrEmpty(deliveryPhone) && rawToken != null)
            {
                RegisterSmsPostCommitAction(dto, deliveryPhone, tenant.Name, tenant.Id, rawToken);
            }
        }

        return dto;
    }

    // ════════════════════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the system-wide TENANT role, creating it on first use if the database has
    /// not yet been seeded (e.g. integration/unit test environments).
    /// The role is company-agnostic (CompanyId == null) and IsSystem == true.
    /// </summary>
    private async Task<Role> GetOrCreateTenantRoleAsync(CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(
                r => r.Code == "TENANT" && r.IsSystem && r.CompanyId == null && r.DeletedAt == null,
                cancellationToken);

        if (role == null)
        {
            role = new Role
            {
                Id = Guid.CreateVersion7(),
                Code = "TENANT",
                NameEn = "Tenant",
                NameAr = "مستأجر",
                IsSystem = true,
                CompanyId = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.Roles.Add(role);
        }

        return role;
    }

    /// <summary>
    /// Ensures a TENANT UserCompanyRole entry exists for the user in this company.
    /// Idempotent: a second call for the same (user, company, role) triple is a no-op.
    /// </summary>
    private async Task EnsureUserCompanyRoleAsync(
        Guid userId, Guid companyId, Guid roleId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.UserCompanyRoles
            .AnyAsync(ucr => ucr.UserId == userId
                          && ucr.CompanyId == companyId
                          && ucr.RoleId == roleId
                          && ucr.DeletedAt == null,
                      cancellationToken);

        if (!exists)
            _dbContext.UserCompanyRoles.Add(BuildMembership(userId, companyId, roleId));
    }

    private static UserCompanyRole BuildMembership(Guid userId, Guid companyId, Guid roleId) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CompanyId = companyId,
            RoleId = roleId,
            Status = MembershipStatus.InvitedPending,
            InvitedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    private static ProvisionTenantAccountResponseDto BuildResponseDto(
        Guid tenantId, Guid userId, Guid companyId, string? rawToken, DateTimeOffset expiresAt) =>
        new()
        {
            TenantId = tenantId,
            UserId = userId,
            CompanyId = companyId,
            ActivationToken = rawToken,
            ExpiresAt = expiresAt,
            EmailSent = false,
            SmsSent = false
        };

    /// <summary>
    /// Registers an activation-email dispatch as a post-commit action.
    /// Executes ONLY after the database transaction commits successfully.
    /// A Brevo failure sets EmailSent = false but never rolls back the committed account.
    /// </summary>
    private void RegisterEmailPostCommitAction(
        ProvisionTenantAccountResponseDto dto,
        string recipientEmail,
        string tenantName,
        Guid tenantId,
        string rawToken)
    {
        _logger.LogInformation(
            "[DIAG:ProvisionTenantAccount] Post-commit email callback registered for TenantId={TenantId}.", tenantId);

        _postCommitRegistrar.RegisterPostCommitAction(async ct =>
        {
            _logger.LogInformation(
                "[DIAG:ProvisionTenantAccount] Post-commit email callback started for TenantId={TenantId}.", tenantId);
            try
            {
                dto.EmailSent = await _emailSender.SendTenantActivationEmailAsync(
                    recipientEmail: recipientEmail,
                    tenantName: tenantName,
                    activationToken: rawToken,
                    cancellationToken: ct);

                _logger.LogInformation(
                    "[DIAG:ProvisionTenantAccount] Post-commit email delivery finished for TenantId={TenantId}. EmailSent={EmailSent}",
                    tenantId, dto.EmailSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[DIAG:ProvisionTenantAccount] Post-commit activation email exception for TenantId={TenantId}. ExceptionType={ExceptionType} Message={Message}. Account remains committed. EmailSent=false.",
                    tenantId, ex.GetType().Name, ex.Message);
                dto.EmailSent = false;
            }
        });
    }

    /// <summary>
    /// Registers an activation-SMS dispatch as a post-commit action.
    /// Executes ONLY after the database transaction commits successfully.
    /// A Twilio failure sets SmsSent = false but never rolls back the committed account.
    /// </summary>
    private void RegisterSmsPostCommitAction(
        ProvisionTenantAccountResponseDto dto,
        string recipientPhone,
        string tenantName,
        Guid tenantId,
        string rawToken)
    {
        _logger.LogInformation(
            "[DIAG:ProvisionTenantAccount] Post-commit SMS callback registered for TenantId={TenantId}.", tenantId);

        _postCommitRegistrar.RegisterPostCommitAction(async ct =>
        {
            _logger.LogInformation(
                "[DIAG:ProvisionTenantAccount] Post-commit SMS callback started for TenantId={TenantId}.", tenantId);
            try
            {
                dto.SmsSent = await _smsSender.SendTenantActivationSmsAsync(
                    recipientPhone: recipientPhone,
                    tenantName: tenantName,
                    activationToken: rawToken,
                    cancellationToken: ct);

                _logger.LogInformation(
                    "[DIAG:ProvisionTenantAccount] Post-commit SMS delivery finished for TenantId={TenantId}. SmsSent={SmsSent}",
                    tenantId, dto.SmsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[DIAG:ProvisionTenantAccount] Post-commit activation SMS exception for TenantId={TenantId}. ExceptionType={ExceptionType} Message={Message}. Account remains committed. SmsSent=false.",
                    tenantId, ex.GetType().Name, ex.Message);
                dto.SmsSent = false;
            }
        });
    }

    // ── Token helpers ────────────────────────────────────────────────────────────────

    private static string GenerateRawToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string rawToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(rawToken.Trim());
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    // ── Identity normalisation ───────────────────────────────────────────────────────

    /// <summary>Trims the phone string. Returns null for empty/whitespace inputs.</summary>
    private static string? NormalizePhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

    /// <summary>Trims and lowercases the email string. Returns null for empty/whitespace inputs.</summary>
    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
