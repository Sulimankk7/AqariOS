using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.ExpireLeaseContract;

public record ExpireLeaseContractCommand(
    Guid ContractId,
    DateTimeOffset? AsOf = null
) : ICommand;
