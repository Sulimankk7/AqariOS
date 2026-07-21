using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.RemoveListingImage;

public record RemoveListingImageCommand(
    Guid ListingId,
    Guid ImageId
) : ICommand;
