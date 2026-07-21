using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Common.Enums;

namespace PropertyOS.Application.Marketplace.Commands.UpdateMarketplaceListing;

public record UpdateMarketplaceListingCommand(
    Guid Id,
    string Title,
    string Description,
    decimal MonthlyRent,
    decimal? SecurityDeposit,
    CurrencyCode Currency,
    string ContactPhone,
    string? ContactWhatsapp,
    DateOnly? ExpirationDate,
    bool IsFeatured
) : ICommand;
