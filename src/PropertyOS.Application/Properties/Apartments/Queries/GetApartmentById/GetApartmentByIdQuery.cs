using System;
using MediatR;
using PropertyOS.Application.Properties.Apartments.Queries.Common;

namespace PropertyOS.Application.Properties.Apartments.Queries.GetApartmentById;

public record GetApartmentByIdQuery(Guid Id) : IRequest<ApartmentDto?>;
