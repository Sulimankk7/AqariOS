using FluentValidation;

namespace PropertyOS.Application.Dashboard.Queries.GetDashboardSummary;

/// <summary>
/// Validator for GetDashboardSummaryQuery (currently empty per CQRS specification).
/// </summary>
public class GetDashboardSummaryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryValidator()
    {
        // Parameterless query validation placeholder
    }
}
