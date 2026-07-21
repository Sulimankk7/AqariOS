using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.CreateViewingRequest;

public record CreateViewingRequestCommand(
    Guid ListingId,
    string ApplicantName,
    string PhoneNumber,
    string? Email,
    DateOnly? PreferredViewingDate,
    string? Notes
) : ICommand<Guid>;
