using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Marketplace.Queries.Common;
using PropertyOS.Domain.Marketplace.Enums;

namespace PropertyOS.Application.Marketplace.Queries.GetViewingRequests;

public record GetViewingRequestsQuery(
    Guid? ListingId = null,
    ViewingRequestStatus? Status = null,
    DateTimeOffset? LastSeenSubmittedAt = null,
    Guid? LastSeenId = null,
    int PageSize = 10
) : IRequest<List<ViewingRequestSummaryDto>>;
