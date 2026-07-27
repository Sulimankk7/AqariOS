using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.SearchTenants;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class SearchTenantsQueryHandlerTests
{
    [Fact]
    public async Task Handle_PassesCompanyScopeAndSearchTerm_ReturnsRepositoryResult()
    {
        var tenantCtx = new FakeTenantContext();
        var expected = new List<TenantDto> { new() { Id = Guid.NewGuid(), Name = "Ahmad" } };
        var repo = new FakeTenantRepository { SearchResult = expected };

        var handler = new SearchTenantsQueryHandler(repo, tenantCtx);

        var result = await handler.Handle(new SearchTenantsQuery("ahmad"), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(tenantCtx.CompanyId, repo.LastSearchCompanyId);
        Assert.Equal("ahmad", repo.LastSearchTerm);
    }

    [Fact]
    public async Task Handle_NullCompanyContext_ThrowsInvalidOperationException()
    {
        var tenantCtx = new FakeTenantContext { CompanyId = null };
        var handler = new SearchTenantsQueryHandler(new FakeTenantRepository(), tenantCtx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new SearchTenantsQuery(""), CancellationToken.None));
    }
}
