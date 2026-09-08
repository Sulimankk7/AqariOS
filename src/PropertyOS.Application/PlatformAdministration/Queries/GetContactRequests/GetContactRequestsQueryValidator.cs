using FluentValidation;
using PropertyOS.Domain.PlatformAdministration.Enums;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetContactRequests;
public sealed class GetContactRequestsQueryValidator : AbstractValidator<GetContactRequestsQuery>
{
    public GetContactRequestsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(x => string.IsNullOrWhiteSpace(x) || Enum.TryParse<ContactRequestStatus>(x, true, out _)).WithMessage("Invalid contact request status.");
    }
}
