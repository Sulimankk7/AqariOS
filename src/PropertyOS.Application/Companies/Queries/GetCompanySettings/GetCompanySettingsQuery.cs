using System;
using MediatR;
using PropertyOS.Application.Companies.Queries.Common;

namespace PropertyOS.Application.Companies.Queries.GetCompanySettings;

public record GetCompanySettingsQuery(Guid CompanyId) : IRequest<CompanySettingsDto?>;
