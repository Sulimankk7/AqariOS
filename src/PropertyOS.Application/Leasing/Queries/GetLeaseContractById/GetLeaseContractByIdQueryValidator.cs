using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseContractById;

public class GetLeaseContractByIdQueryValidator : AbstractValidator<GetLeaseContractByIdQuery>
{
    public GetLeaseContractByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Contract ID is required.");
    }
}
