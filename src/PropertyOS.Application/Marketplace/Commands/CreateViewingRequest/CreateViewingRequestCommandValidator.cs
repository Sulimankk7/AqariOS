using System;
using FluentValidation;

namespace PropertyOS.Application.Marketplace.Commands.CreateViewingRequest;

public class CreateViewingRequestCommandValidator : AbstractValidator<CreateViewingRequestCommand>
{
    public CreateViewingRequestCommandValidator()
    {
        RuleFor(x => x.ListingId)
            .NotEmpty()
            .WithMessage("Listing ID must be specified.");

        RuleFor(x => x.ApplicantName)
            .NotEmpty()
            .WithMessage("Applicant name must not be blank.")
            .MaximumLength(100)
            .WithMessage("Applicant name must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email format is invalid.")
            .MaximumLength(100)
            .WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.PreferredViewingDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.PreferredViewingDate.HasValue)
            .WithMessage("Preferred viewing date cannot be in the past.");
    }
}
