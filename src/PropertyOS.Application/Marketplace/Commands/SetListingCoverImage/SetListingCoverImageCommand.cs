using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.SetListingCoverImage;

public record SetListingCoverImageCommand(
    Guid ListingId,
    Guid ImageId
) : ICommand;
