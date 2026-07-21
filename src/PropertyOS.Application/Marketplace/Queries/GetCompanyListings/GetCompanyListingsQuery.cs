using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Marketplace.Queries.Common;
using PropertyOS.Domain.Marketplace.Enums;

namespace PropertyOS.Application.Marketplace.Queries.GetCompanyListings;

public record GetCompanyListingsQuery(
    ListingStatus? Status = null,
    Guid? BuildingId = null,
    Guid? ApartmentId = null,
    string? SearchText = null,
    DateOnly? LastSeenPublishedDate = null,
    Guid? LastSeenId = null,
    int PageSize = 10
) : IRequest<List<CompanyListingSummaryDto>>;
