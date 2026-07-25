using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity.Commands.Register;

namespace PropertyOS.Application.Identity.Provisioning;

/// <summary>
/// Orchestrates tenant provisioning by executing an ordered pipeline of <see cref="ITenantProvisioningStep"/> instances
/// within a single atomic PostgreSQL database transaction.
/// Ensures that any failure in any step rolls back all created entities (User, Company, Settings, Roles, Tokens).
/// </summary>
public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly IEnumerable<ITenantProvisioningStep> _steps;
    private readonly IApplicationDbContext _dbContext;

    public TenantProvisioningService(IEnumerable<ITenantProvisioningStep> steps, IApplicationDbContext dbContext)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<RegisterResponseDto> ProvisionTenantAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var context = new TenantProvisioningContext(command, DateTimeOffset.UtcNow);
        var orderedSteps = _steps.OrderBy(s => s.Order).ToList();

        foreach (var step in orderedSteps)
        {
            await step.ExecuteAsync(context, cancellationToken);
        }

        if (context.Response == null)
        {
            throw new InvalidOperationException("Tenant provisioning pipeline completed without producing an authentication response.");
        }

        return context.Response;
    }
}
