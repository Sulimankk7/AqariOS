using System;
using System.Collections.Generic;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseContractById;

public class LeaseContractDetailDto : LeaseContractDto
{
    public List<ContractStatusHistoryDto> StatusHistory { get; set; } = new();
    public List<ContractDocumentDto> Documents { get; set; } = new();
    public ContractTerminationDto? Termination { get; set; }
}
