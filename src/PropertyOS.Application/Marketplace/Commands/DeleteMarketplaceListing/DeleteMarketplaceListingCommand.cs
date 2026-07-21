using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.DeleteMarketplaceListing;

public record DeleteMarketplaceListingCommand(Guid Id) : ICommand;
