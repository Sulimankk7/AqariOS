using System;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

/// <summary>
/// Direct coverage of the Module 6 doc §6.1 grace-period-aware settlement matrix.
/// </summary>
public class AllocationSettlementTests
{
    private static readonly DateOnly DueDate = new(2026, 3, 1);

    [Theory]
    [InlineData(1000, 1000)]
    [InlineData(1200, 1000)] // overpayment still resolves to Paid
    public void DeriveStatus_FullyPaid_ReturnsPaid(decimal paid, decimal due)
    {
        var status = AllocationSettlement.DeriveStatus(paid, due, DueDate, DueDate.AddDays(30), graceDays: 0);
        Assert.Equal(DueDateStatus.Paid, status);
    }

    [Fact]
    public void DeriveStatus_PartialWithinGrace_ReturnsPartiallyPaid()
    {
        // grace 5: due 03-01, today 03-06 == due+grace -> still within grace
        var status = AllocationSettlement.DeriveStatus(400, 1000, DueDate, DueDate.AddDays(5), graceDays: 5);
        Assert.Equal(DueDateStatus.PartiallyPaid, status);
    }

    [Fact]
    public void DeriveStatus_PartialPastGrace_ReturnsLate()
    {
        var status = AllocationSettlement.DeriveStatus(400, 1000, DueDate, DueDate.AddDays(6), graceDays: 5);
        Assert.Equal(DueDateStatus.Late, status);
    }

    [Fact]
    public void DeriveStatus_UnpaidWithinGrace_ReturnsPending()
    {
        var status = AllocationSettlement.DeriveStatus(0, 1000, DueDate, DueDate.AddDays(5), graceDays: 5);
        Assert.Equal(DueDateStatus.Pending, status);
    }

    [Fact]
    public void DeriveStatus_UnpaidPastGrace_ReturnsOverdueUnpaid()
    {
        var status = AllocationSettlement.DeriveStatus(0, 1000, DueDate, DueDate.AddDays(6), graceDays: 5);
        Assert.Equal(DueDateStatus.OverdueUnpaid, status);
    }

    [Fact]
    public void DeriveStatus_ZeroGrace_BoundaryIsDueDateItself()
    {
        // On the due date: not yet late. One day after: overdue.
        Assert.Equal(DueDateStatus.Pending, AllocationSettlement.DeriveStatus(0, 1000, DueDate, DueDate, 0));
        Assert.Equal(DueDateStatus.OverdueUnpaid, AllocationSettlement.DeriveStatus(0, 1000, DueDate, DueDate.AddDays(1), 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(400)]
    public void DeriveStatus_NullDueDate_NeverLateOrOverdue(decimal paid)
    {
        var status = AllocationSettlement.DeriveStatus(paid, 1000, null, DueDate.AddYears(10), graceDays: 0);
        Assert.Equal(paid > 0 ? DueDateStatus.PartiallyPaid : DueDateStatus.Pending, status);
    }
}
