using System;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Development;

public record SeedResult(
    Guid CompanyId,
    Guid TenantUserId,
    Guid TenantId,
    Guid LeaseContractId,
    Guid RentPaymentId,
    Guid PaymentSubmissionId,
    Guid ProofFileId,
    string ReferenceNumber,
    string SubmissionStatus,
    string DueDateStatus,
    decimal AmountPaid);

public interface IDevelopmentPaymentVerificationSeedService
{
    Task<SeedResult> SeedAsync(Guid companyId, Guid currentOwnerId, CancellationToken cancellationToken = default);
}
