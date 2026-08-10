using System.Collections.Generic;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentById;

public class RentPaymentDetailDto : RentPaymentDto
{
    public ChequeDetailDto? ChequeDetails { get; set; }
    public List<PaymentAllocationDto> IncomingAllocations { get; set; } = new();
    public List<PaymentAllocationDto> OutgoingAllocations { get; set; } = new();
    public List<PaymentSubmissionDto> Submissions { get; set; } = new();
}
