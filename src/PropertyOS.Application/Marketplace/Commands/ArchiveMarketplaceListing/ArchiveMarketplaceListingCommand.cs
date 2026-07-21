using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.ArchiveMarketplaceListing;

public record ArchiveMarketplaceListingCommand(Guid Id) : ICommand;
