using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.DeleteExpense;

public record DeleteExpenseCommand(Guid Id) : ICommand;
