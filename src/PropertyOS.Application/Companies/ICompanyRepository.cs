using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Companies.Queries.Common;
using PropertyOS.Domain.Companies;

namespace PropertyOS.Application.Companies;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CompanyDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CompanySettingsDto?> GetSettingsByIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Company?> GetWithSettingsByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
