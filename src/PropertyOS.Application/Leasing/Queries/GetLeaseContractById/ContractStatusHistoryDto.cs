using System;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseContractById;

public class ContractStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid LeaseContractId { get; set; }
    public ContractStatus? PreviousStatus { get; set; }
    public ContractStatus NewStatus { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string? Reason { get; set; }
}
