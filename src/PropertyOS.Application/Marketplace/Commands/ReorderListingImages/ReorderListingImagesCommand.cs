using System;
using System.Collections.Generic;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.ReorderListingImages;

public record ReorderListingImagesCommand(
    Guid ListingId,
    List<Guid> ImageIds
) : ICommand;
