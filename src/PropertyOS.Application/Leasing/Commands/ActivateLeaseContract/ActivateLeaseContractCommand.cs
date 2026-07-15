using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.ActivateLeaseContract;

public record ActivateLeaseContractCommand(Guid ContractId) : ICommand;
