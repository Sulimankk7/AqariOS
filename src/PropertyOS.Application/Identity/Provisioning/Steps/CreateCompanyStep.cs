using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Companies;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

/// <summary>
/// Provisioning step 2: Instantiates the root tenant <see cref="Company"/> entity.
/// </summary>
public class CreateCompanyStep : ITenantProvisioningStep
{
    private readonly IApplicationDbContext _dbContext;

    public CreateCompanyStep(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public int Order => 20;

    public Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        var cmd = context.Command;
        var user = context.User ?? throw new InvalidOperationException("User entity must be populated before creating Company.");

        var company = Company.Create(
            legalName: cmd.CompanyName,
            displayName: string.IsNullOrWhiteSpace(cmd.DisplayName) ? cmd.CompanyName : cmd.DisplayName,
            primaryPhone: NormalizePrimaryPhone(cmd.Phone),
            companyType: cmd.CompanyType,
            countryCode: string.IsNullOrWhiteSpace(cmd.CountryCode) ? "JO" : cmd.CountryCode,
            createdAt: context.CreatedAt,
            createdBy: user.Id,
            primaryEmail: cmd.Email);

        company.MarkPendingApproval(context.CreatedAt, user.Id);

        _dbContext.Companies.Add(company);
        context.Company = company;

        return Task.CompletedTask;
    }

    private static string NormalizePrimaryPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return "+962790000000";
        }

        var trimmed = phone.Trim();

        if (trimmed.StartsWith("+962"))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("962"))
        {
            return "+" + trimmed;
        }

        if (trimmed.StartsWith("0"))
        {
            return "+962" + trimmed.Substring(1);
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[0-9]{8,9}$"))
        {
            return "+962" + trimmed;
        }

        return trimmed;
    }
}
