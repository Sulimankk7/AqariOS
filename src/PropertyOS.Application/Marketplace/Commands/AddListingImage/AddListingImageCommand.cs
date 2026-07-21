using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.AddListingImage;

public record AddListingImageCommand(
    Guid ListingId,
    Guid FileId,
    bool IsCover
) : ICommand<Guid>;
