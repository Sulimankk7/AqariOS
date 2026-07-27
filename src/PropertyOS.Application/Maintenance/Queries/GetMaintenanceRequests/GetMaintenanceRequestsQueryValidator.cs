using FluentValidation;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceRequests;

public class GetMaintenanceRequestsQueryValidator : AbstractValidator<GetMaintenanceRequestsQuery>
{
    public GetMaintenanceRequestsQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");
    }
}
