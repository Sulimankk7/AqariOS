using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.GenerateScheduledInstallments;

public record GenerateScheduledInstallmentsCommand(Guid LeaseContractId) : ICommand;
