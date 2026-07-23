using System;
using MediatR;
using PropertyOS.Application.Companies.Queries.Common;

namespace PropertyOS.Application.Companies.Queries.GetCompanyById;

public record GetCompanyByIdQuery(Guid Id) : IRequest<CompanyDetailDto?>;
