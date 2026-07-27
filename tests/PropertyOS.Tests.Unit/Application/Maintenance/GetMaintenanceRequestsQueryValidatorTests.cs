using FluentValidation.TestHelper;
using PropertyOS.Application.Maintenance.Queries.GetMaintenanceRequests;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Maintenance;

public class GetMaintenanceRequestsQueryValidatorTests
{
    private readonly GetMaintenanceRequestsQueryValidator _validator = new();

    [Fact]
    public void Validator_Should_Pass_For_Default_Query()
    {
        var result = _validator.TestValidate(new GetMaintenanceRequestsQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(200)]
    public void Validator_Should_Pass_For_PageSize_InRange(int pageSize)
    {
        var result = _validator.TestValidate(new GetMaintenanceRequestsQuery(PageSize: pageSize));
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(201)]
    public void Validator_Should_Fail_For_PageSize_OutOfRange(int pageSize)
    {
        var result = _validator.TestValidate(new GetMaintenanceRequestsQuery(PageSize: pageSize));
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}
