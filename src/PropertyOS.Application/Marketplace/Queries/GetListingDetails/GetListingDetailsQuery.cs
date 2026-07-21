using System;
using MediatR;
using PropertyOS.Application.Marketplace.Queries.Common;

namespace PropertyOS.Application.Marketplace.Queries.GetListingDetails;

public record GetListingDetailsQuery(Guid Id) : IRequest<ListingDetailDto?>;
