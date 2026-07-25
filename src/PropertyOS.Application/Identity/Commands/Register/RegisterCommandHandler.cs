using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity.Provisioning;

namespace PropertyOS.Application.Identity.Commands.Register;

/// <summary>
/// Command handler orchestrating tenant registration validation, strategy policy checks, and tenant provisioning.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponseDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantProvisioningService _provisioningService;

    public RegisterCommandHandler(
        IApplicationDbContext dbContext,
        ITenantProvisioningService provisioningService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _provisioningService = provisioningService ?? throw new ArgumentNullException(nameof(provisioningService));
    }

    public async Task<RegisterResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        // 1. Validate email uniqueness
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailNormalized = request.Email.Trim().ToLowerInvariant();
            var emailExists = await _dbContext.Users
                .AnyAsync(u => u.Email != null && u.Email.ToLower() == emailNormalized && u.DeletedAt == null, cancellationToken);

            if (emailExists)
            {
                throw new DuplicateEmailException(request.Email.Trim());
            }
        }

        // 2. Validate phone uniqueness
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phoneNormalized = request.Phone.Trim();
            var phoneExists = await _dbContext.Users
                .AnyAsync(u => u.Phone != null && u.Phone == phoneNormalized && u.DeletedAt == null, cancellationToken);

            if (phoneExists)
            {
                throw new DuplicatePhoneException(request.Phone.Trim());
            }
        }

        // 3. Delegate tenant provisioning to orchestration pipeline
        return await _provisioningService.ProvisionTenantAsync(request, cancellationToken);
    }
}
