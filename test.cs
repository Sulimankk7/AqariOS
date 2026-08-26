using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Entities;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Persistence;

public class Program
{
    public static async Task Main()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        using var context = new ApplicationDbContext(options);

        // 1. Setup entities
        var obligation = new RentPayment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment, 320m, "JOD", null, null,
            null, DateOnly.FromDateTime(DateTime.UtcNow));
        obligation.SetId(Guid.NewGuid());

        var receiving1 = new RentPayment(
            obligation.CompanyId, obligation.TenantId, obligation.BuildingId, obligation.ApartmentId,
            PaymentPurpose.UnallocatedReceipt, 150m, "JOD", null, null,
            null, null);
        receiving1.SetId(Guid.NewGuid());
        
        var receiving2 = new RentPayment(
            obligation.CompanyId, obligation.TenantId, obligation.BuildingId, obligation.ApartmentId,
            PaymentPurpose.UnallocatedReceipt, 170m, "JOD", null, null,
            null, null);
        receiving2.SetId(Guid.NewGuid());

        var receipt1 = receiving1.IssueReceipt(
            "REC-000018", DateTimeOffset.UtcNow, Guid.NewGuid(), "notes", Guid.NewGuid(), 150m);
            
        var receipt2 = receiving2.IssueReceipt(
            "REC-000019", DateTimeOffset.UtcNow, Guid.NewGuid(), "notes", Guid.NewGuid(), 170m);

        var alloc1 = PaymentAllocation.Create(
            obligation.CompanyId, receiving1.Id, obligation.Id, 150m,
            DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());
            
        var alloc2 = PaymentAllocation.Create(
            obligation.CompanyId, receiving2.Id, obligation.Id, 170m,
            DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());

        context.RentPayments.AddRange(obligation, receiving1, receiving2);
        // IssueReceipt adds the receipt to _receipts which is a navigation property but we might need to add it manually if EF doesn't pick it up
        context.RentPaymentReceipts.AddRange(receiving1.Receipt, receiving2.Receipt);
        context.PaymentAllocations.AddRange(alloc1, alloc2);
        
        await context.SaveChangesAsync();

        var query = from a in context.PaymentAllocations.AsNoTracking()
                    where a.ObligationPaymentId == obligation.Id
                       && a.AllocationStatus == AllocationStatus.Active
                       && a.DeletedAt == null
                    join rcv in context.RentPayments.AsNoTracking() on a.ReceivingPaymentId equals rcv.Id
                    join r in context.RentPaymentReceipts.AsNoTracking() on rcv.Id equals r.RentPaymentId into rs
                    from r in rs.Where(x => x.DeletedAt == null).DefaultIfEmpty()
                    orderby a.AllocationDate, a.CreatedAt
                    select new
                    {
                        AllocatedAmount = a.AllocatedAmount,
                        ReceiptAmount = r != null ? (decimal?)r.Amount : null,
                        ReceiptNumber = r != null ? r.ReceiptNumber : null
                    };

        var result = await query.ToListAsync();
        
        foreach(var res in result)
        {
            Console.WriteLine($"ReceiptNumber: {res.ReceiptNumber}, AllocatedAmount: {res.AllocatedAmount}, ReceiptAmount: {res.ReceiptAmount}");
        }
    }
}

public static class TestExtensions {
    public static void SetId(this RentPayment rp, Guid id) {
        typeof(RentPayment).GetProperty("Id").SetValue(rp, id);
    }
}
