using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Domain.Leasing;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Infrastructure.Persistence;

public class TenantPhoneIndexConfigurationTests
{
    [Fact]
    public void Model_DefinesGlobalPartialUniqueActiveTenantPhoneIndex()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new PropertyOsDbContext(options);

        var index = dbContext.Model.FindEntityType(typeof(Tenant))!
            .GetIndexes()
            .Single(candidate => candidate.GetDatabaseName() == "uq_tenants_phone_active");

        Assert.True(index.IsUnique);
        Assert.Equal("phone IS NOT NULL AND deleted_at IS NULL", index.GetFilter());
        Assert.Equal(new[] { nameof(Tenant.Phone) }, index.Properties.Select(property => property.Name));
    }
}
