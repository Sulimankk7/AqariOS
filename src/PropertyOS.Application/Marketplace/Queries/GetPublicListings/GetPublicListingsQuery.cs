using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Marketplace.Queries.Common;

namespace PropertyOS.Application.Marketplace.Queries.GetPublicListings;

public record GetPublicListingsQuery(
    Guid? BuildingId = null,
    Guid? ApartmentId = null,
    string? SearchText = null,
    bool? LastSeenIsFeatured = null,
    DateOnly? LastSeenPublishedDate = null,
    Guid? LastSeenId = null,
    int PageSize = 10
) : IRequest<List<PublicListingSummaryDto>>;
