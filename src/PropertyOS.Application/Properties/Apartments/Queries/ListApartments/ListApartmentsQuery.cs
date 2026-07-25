using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Properties.Apartments.Queries.Common;

namespace PropertyOS.Application.Properties.Apartments.Queries.ListApartments;

public record ListApartmentsQuery(Guid? BuildingId = null, Guid? FloorId = null) : IRequest<List<ApartmentDto>>;
