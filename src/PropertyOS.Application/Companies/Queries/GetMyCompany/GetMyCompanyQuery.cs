using MediatR;
using PropertyOS.Application.Companies.Queries.Common;

namespace PropertyOS.Application.Companies.Queries.GetMyCompany;

public record GetMyCompanyQuery() : IRequest<CompanyDetailDto?>;
