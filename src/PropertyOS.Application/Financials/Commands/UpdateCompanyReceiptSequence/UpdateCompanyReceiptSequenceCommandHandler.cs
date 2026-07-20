using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Application.Financials.Commands.UpdateCompanyReceiptSequence;

public class UpdateCompanyReceiptSequenceCommandHandler : IRequestHandler<UpdateCompanyReceiptSequenceCommand, Unit>
{
    private readonly ICompanyReceiptSequenceRepository _sequenceRepository;
    private readonly ITenantContext _tenantContext;

    public UpdateCompanyReceiptSequenceCommandHandler(
        ICompanyReceiptSequenceRepository sequenceRepository,
        ITenantContext tenantContext)
    {
        _sequenceRepository = sequenceRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Unit> Handle(UpdateCompanyReceiptSequenceCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var sequence = await _sequenceRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        if (sequence == null)
        {
            sequence = CompanyReceiptSequence.Create(
                companyId: companyId,
                prefix: request.Prefix,
                paddingLength: request.PaddingLength,
                resetPolicy: request.ResetPolicy,
                now: DateTimeOffset.UtcNow
            );
            await _sequenceRepository.AddAsync(sequence, cancellationToken);
        }
        else
        {
            sequence.UpdateFormatting(
                prefix: request.Prefix,
                paddingLength: request.PaddingLength,
                resetPolicy: request.ResetPolicy,
                now: DateTimeOffset.UtcNow
            );
        }

        return Unit.Value;
    }
}
