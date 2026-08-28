using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

/// <summary>
/// Provisioning step 1: Hashes the user password using Argon2id and creates the initial <see cref="User"/> entity.
/// </summary>
public class CreateUserStep : ITenantProvisioningStep
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserStep(IApplicationDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public int Order => 10;

    public Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        var cmd = context.Command;
        var hashedPassword = _passwordHasher.HashPassword(cmd.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = string.IsNullOrWhiteSpace(cmd.Email) ? null : cmd.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(cmd.Phone) ? null : cmd.Phone.Trim(),
            PasswordHash = hashedPassword,
            PasswordAlgorithm = "argon2id",
            FullName = cmd.FullName.Trim(),
            PreferredLanguage = string.IsNullOrWhiteSpace(cmd.PreferredLanguage) ? "ar" : cmd.PreferredLanguage,
            IsActive = false,
            CreatedAt = context.CreatedAt,
            UpdatedAt = context.CreatedAt
        };

        _dbContext.Users.Add(user);
        context.User = user;

        return Task.CompletedTask;
    }
}
