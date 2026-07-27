using System;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Leasing;

public class LeaseContractTests
{
    private LeaseContract CreateTestContract(ContractStatus status)
    {
        return LeaseContract.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "LC-123", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, status);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingSignature)]
    public void Activate_AllowedStatuses_Succeeds(ContractStatus status)
    {
        var contract = CreateTestContract(status);
        contract.Activate(DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(ContractStatus.Active, contract.Status);
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Superseded)]
    public void Activate_DisallowedStatuses_Throws(ContractStatus status)
    {
        var contract = CreateTestContract(status);
        Assert.Throws<InvalidOperationException>(() => contract.Activate(DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Equal(status, contract.Status);
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    public void Terminate_AllowedStatuses_Succeeds(ContractStatus status)
    {
        var contract = CreateTestContract(status);
        contract.Terminate(DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(ContractStatus.Terminated, contract.Status);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingSignature)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Superseded)]
    public void Terminate_DisallowedStatuses_Throws(ContractStatus status)
    {
        var contract = CreateTestContract(status);
        Assert.Throws<InvalidOperationException>(() => contract.Terminate(DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Equal(status, contract.Status);
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expired)]
    public void SupersedeForRenewal_AllowedStatuses_Succeeds(ContractStatus status)
    {
        var contract = CreateTestContract(status);
        contract.SupersedeForRenewal(DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(ContractStatus.Superseded, contract.Status);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingSignature)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Superseded)]
    public void SupersedeForRenewal_DisallowedStatuses_Throws(ContractStatus status)
    {
        var contract = CreateTestContract(status);
        Assert.Throws<InvalidOperationException>(() => contract.SupersedeForRenewal(DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Equal(status, contract.Status);
    }

    [Fact]
    public void UpdateDraftTerms_DraftStatus_SucceedsAndPreservesLockedFields()
    {
        var contract = CreateTestContract(ContractStatus.Draft);
        var originalId = contract.Id;
        var originalCompanyId = contract.CompanyId;
        var originalContractNumber = contract.ContractNumber;
        var originalCurrency = contract.Currency;

        var newApartmentId = Guid.NewGuid();
        var newBuildingId = Guid.NewGuid();
        var newTenantId = Guid.NewGuid();
        var newStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var newEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));

        contract.UpdateDraftTerms(newApartmentId, newBuildingId, newTenantId, newStart, newEnd, 500, 200, PaymentFrequency.Quarterly, 15, LegalRegime.Standard, TenantType.Corporate, "Updated Notes", DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.Equal(originalId, contract.Id);
        Assert.Equal(originalCompanyId, contract.CompanyId);
        Assert.Equal(originalContractNumber, contract.ContractNumber);
        Assert.Equal(originalCurrency, contract.Currency);
        Assert.Equal(newApartmentId, contract.ApartmentId);
        Assert.Equal(newBuildingId, contract.BuildingId);
        Assert.Equal(newTenantId, contract.TenantId);
        Assert.Equal(newStart, contract.StartDate);
        Assert.Equal(newEnd, contract.EndDate);
        Assert.Equal(500, contract.MonthlyRentAmount);
        Assert.Equal(200, contract.SecurityDepositAmount);
        Assert.Equal(PaymentFrequency.Quarterly, contract.PaymentFrequency);
        Assert.Equal(15, contract.PaymentDueDay);
        Assert.Equal("Updated Notes", contract.Notes);
    }

    [Theory]
    [InlineData(ContractStatus.PendingSignature)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Superseded)]
    public void UpdateDraftTerms_DisallowedStatuses_ThrowsInvalidOperationException(ContractStatus status)
    {
        var contract = CreateTestContract(status);
        Assert.Throws<InvalidOperationException>(() => contract.UpdateDraftTerms(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            100, 100, PaymentFrequency.Monthly, 1, LegalRegime.Standard, TenantType.Personal, "Notes", DateTimeOffset.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void UpdateDraftTerms_InvalidDates_ThrowsInvalidOperationException()
    {
        var contract = CreateTestContract(ContractStatus.Draft);
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
        var end = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => contract.UpdateDraftTerms(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            start, end, 100, 100, PaymentFrequency.Monthly, 1, LegalRegime.Standard, TenantType.Personal, "Notes", DateTimeOffset.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void Expire_ActiveStatus_TransitionsToExpired()
    {
        var contract = CreateTestContract(ContractStatus.Active);
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        contract.Expire(now, userId);

        Assert.Equal(ContractStatus.Expired, contract.Status);
        Assert.Equal(now, contract.UpdatedAt);
        Assert.Equal(userId, contract.UpdatedBy);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingSignature)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Superseded)]
    public void Expire_DisallowedStatuses_ThrowsInvalidOperationException(ContractStatus status)
    {
        var contract = CreateTestContract(status);
        Assert.Throws<InvalidOperationException>(() => contract.Expire(DateTimeOffset.UtcNow, Guid.NewGuid()));
    }
}
