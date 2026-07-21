using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.PublishMarketplaceListing;

public record PublishMarketplaceListingCommand(Guid Id) : ICommand;
