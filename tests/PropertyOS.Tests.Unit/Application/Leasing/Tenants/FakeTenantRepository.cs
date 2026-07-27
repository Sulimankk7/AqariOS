using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetTenantById;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

/// <summary>
/// Self-contained fake for ITenantRepository shared by the tenant handler tests.
/// </summary>
internal class FakeTenantRepository : ITenantRepository
{
    public List<Tenant> Store { get; } = new();
    public List<Tenant> AddedTenants { get; } = new();

    public bool NationalIdExists { get; set; }
    public bool HasNonTerminalLease { get; set; }

    public Guid? LastExistsCompanyId { get; private set; }
    public string? LastExistsNationalId { get; private set; }
    public Guid? LastExistsExcludeTenantId { get; private set; }
    public Guid? LastNonTerminalLeaseTenantId { get; private set; }
    public Guid? LastSearchCompanyId { get; private set; }
    public string? LastSearchTerm { get; private set; }

    public List<TenantDto> SearchResult { get; set; } = new();
    public TenantDetailDto? DetailResult { get; set; }

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.FirstOrDefault(t => t.Id == id && t.DeletedAt == null));

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        AddedTenants.Add(tenant);
        Store.Add(tenant);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByNationalIdAsync(Guid companyId, string nationalId, Guid? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        LastExistsCompanyId = companyId;
        LastExistsNationalId = nationalId;
        LastExistsExcludeTenantId = excludeTenantId;
        return Task.FromResult(NationalIdExists);
    }

    public Task<bool> HasNonTerminalLeaseContractAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        LastNonTerminalLeaseTenantId = tenantId;
        return Task.FromResult(HasNonTerminalLease);
    }

    public Task<TenantDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
        => Task.FromResult(DetailResult);

    public Task<List<TenantDto>> SearchAsync(Guid companyId, string searchTerm, CancellationToken cancellationToken = default)
    {
        LastSearchCompanyId = companyId;
        LastSearchTerm = searchTerm;
        return Task.FromResult(SearchResult);
    }
}

internal class FakeTenantContext : PropertyOS.Application.Common.Interfaces.ITenantContext
{
    public Guid? CompanyId { get; set; } = Guid.NewGuid();
    public bool IsPlatformAdmin { get; set; }
}

internal class FakeCurrentUserContext : PropertyOS.Application.Common.Interfaces.ICurrentUserContext
{
    public Guid? UserId { get; set; } = Guid.NewGuid();
}
