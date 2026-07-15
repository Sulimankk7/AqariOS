using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForApartment;

public record GetLeaseHistoryForApartmentQuery(Guid ApartmentId) : IRequest<List<LeaseContractDto>>;
