using System;
using PropertyOS.Domain.Companies.Enums;

namespace PropertyOS.Application.Companies.Queries.Common;

public class CompanyDetailDto
{
    public Guid Id { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? CommercialRegistrationNo { get; set; }
    public string? TaxNumber { get; set; }
    public CompanyType CompanyType { get; set; }
    public string PrimaryPhone { get; set; } = string.Empty;
    public string? PrimaryEmail { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    
    public CompanySettingsDto? Settings { get; set; }
}
