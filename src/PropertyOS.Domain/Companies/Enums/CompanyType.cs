namespace PropertyOS.Domain.Companies.Enums;

/// <summary>
/// PostgreSQL enum: company_type_enum
/// Labels (snake_case mapped by Npgsql): individual_owner, property_management_company, investment_company
/// </summary>
public enum CompanyType
{
    IndividualOwner,
    PropertyManagementCompany,
    InvestmentCompany
}
