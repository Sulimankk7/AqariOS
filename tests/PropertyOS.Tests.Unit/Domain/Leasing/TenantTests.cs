using System;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Leasing;

public class TenantTests
{
    private static Tenant CreateTestTenant() => Tenant.Create(
        companyId: Guid.NewGuid(),
        name: "Ahmad Odeh",
        nationalId: "9901234567",
        phone: "+962791234567",
        createdAt: DateTimeOffset.UtcNow,
        createdBy: Guid.NewGuid(),
        email: "ahmad.odeh@example.com");

    [Fact]
    public void Create_ValidInput_GeneratesClientSideIdBeforeSave()
    {
        var tenant = CreateTestTenant();

        // Client-generated UUIDv7: the ID must be usable before SaveChanges.
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal(7, tenant.Id.Version);
        Assert.Equal("ahmad.odeh@example.com", tenant.Email);
    }

    [Fact]
    public void Create_TwoTenants_GenerateDistinctIds()
    {
        Assert.NotEqual(CreateTestTenant().Id, CreateTestTenant().Id);
    }

    [Fact]
    public void UpdateDetails_ValidInput_UpdatesFieldsAndAudit()
    {
        var tenant = CreateTestTenant();
        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var updatedBy = Guid.NewGuid();

        tenant.UpdateDetails(
            name: "  New Name  ",
            nationalId: " 2222222222 ",
            phone: " +962790000000 ",
            occupation: "  ",
            employer: "Acme",
            updatedAt: updatedAt,
            updatedBy: updatedBy,
            email: " New.Email@Example.com ");

        Assert.Equal("New Name", tenant.Name);
        Assert.Equal("2222222222", tenant.NationalId);
        Assert.Equal("+962790000000", tenant.Phone);
        Assert.Equal("new.email@example.com", tenant.Email);
        Assert.Null(tenant.Occupation); // whitespace collapses to null
        Assert.Equal("Acme", tenant.Employer);
        Assert.Equal(updatedAt, tenant.UpdatedAt);
        Assert.Equal(updatedBy, tenant.UpdatedBy);
    }

    [Theory]
    [InlineData("", "2222222222", "+962790000000")]
    [InlineData("Name", " ", "+962790000000")]
    [InlineData("Name", "2222222222", "")]
    public void UpdateDetails_MissingRequiredField_ThrowsArgumentException(string name, string nationalId, string phone)
    {
        var tenant = CreateTestTenant();

        Assert.Throws<ArgumentException>(() => tenant.UpdateDetails(
            name, nationalId, phone, null, null, DateTimeOffset.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void UpdateDetails_DeletedTenant_ThrowsInvalidOperationException()
    {
        var tenant = CreateTestTenant();
        tenant.SoftDelete(DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => tenant.UpdateDetails(
            "Name", "2222222222", "+962790000000", null, null, DateTimeOffset.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void SoftDelete_FirstCall_SetsDeletionAudit_SecondCallIsIdempotent()
    {
        var tenant = CreateTestTenant();
        var deletedAt = DateTimeOffset.UtcNow;
        var deletedBy = Guid.NewGuid();

        tenant.SoftDelete(deletedAt, deletedBy);
        tenant.SoftDelete(deletedAt.AddDays(1), Guid.NewGuid()); // no-op

        Assert.Equal(deletedAt, tenant.DeletedAt);
        Assert.Equal(deletedBy, tenant.DeletedBy);
    }
}
