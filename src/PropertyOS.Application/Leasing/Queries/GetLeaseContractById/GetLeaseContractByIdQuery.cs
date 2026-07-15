using System;
using MediatR;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseContractById;

public record GetLeaseContractByIdQuery(Guid Id) : IRequest<LeaseContractDetailDto?>;
