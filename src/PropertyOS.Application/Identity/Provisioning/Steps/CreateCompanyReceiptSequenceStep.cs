using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

/// <summary>
/// Creates the default, concurrency-safe receipt sequence for a newly provisioned company.
/// </summary>
public sealed class CreateCompanyReceiptSequenceStep : ITenantProvisioningStep
{
    private readonly ICompanyReceiptSequenceRepository _sequenceRepository;

    public CreateCompanyReceiptSequenceStep(ICompanyReceiptSequenceRepository sequenceRepository)
    {
        _sequenceRepository = sequenceRepository ?? throw new ArgumentNullException(nameof(sequenceRepository));
    }

    public int Order => 45;

    public async Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        var company = context.Company
            ?? throw new InvalidOperationException("Company entity must be populated before creating its receipt sequence.");

        var sequence = CompanyReceiptSequence.Create(
            company.Id,
            prefix: string.Empty,
            paddingLength: 5,
            resetPolicy: ReceiptResetPolicy.Never,
            now: context.CreatedAt);

        await _sequenceRepository.AddAsync(sequence, cancellationToken);
    }
}
