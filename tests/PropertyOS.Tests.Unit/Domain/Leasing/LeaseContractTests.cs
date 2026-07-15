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
}
