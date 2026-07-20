using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.Common;

public class ExpenseDetailDto : ExpenseDto
{
    public System.Collections.Generic.List<ExpenseReceiptDto> Receipts { get; set; } = new();
}
