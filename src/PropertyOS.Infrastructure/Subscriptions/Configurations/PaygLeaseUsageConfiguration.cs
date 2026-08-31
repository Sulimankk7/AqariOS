using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Subscriptions;

namespace PropertyOS.Infrastructure.Subscriptions.Configurations;

internal sealed class PaygLeaseUsageConfiguration : IEntityTypeConfiguration<PaygLeaseUsage>
{
    public void Configure(EntityTypeBuilder<PaygLeaseUsage> builder)
    {
        builder.ToTable("payg_lease_usage", table =>
        {
            table.HasCheckConstraint("chk_payg_lease_usage_dates", "usage_end > usage_start AND billable_days = usage_end - usage_start");
            table.HasCheckConstraint("chk_payg_lease_usage_amount", "calculated_amount >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.UsagePeriodId).HasColumnName("usage_period_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.LeaseContractId).HasColumnName("lease_contract_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.UsageStart).HasColumnName("usage_start").HasColumnType("date").IsRequired();
        builder.Property(x => x.UsageEnd).HasColumnName("usage_end").HasColumnType("date").IsRequired();
        builder.Property(x => x.BillableDays).HasColumnName("billable_days").IsRequired();
        builder.Property(x => x.CalculatedAmount).HasColumnName("calculated_amount").HasColumnType("numeric(14,3)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne(x => x.UsagePeriod).WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.UsagePeriodId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PropertyOS.Domain.Leasing.LeaseContract>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.LeaseContractId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PropertyOS.Domain.Leasing.Tenant>().WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.TenantId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.UsagePeriodId, x.LeaseContractId })
            .IsUnique().HasDatabaseName("uq_payg_lease_usage_period_lease");
        builder.HasIndex(x => new { x.CompanyId, x.UsagePeriodId })
            .HasDatabaseName("idx_payg_lease_usage_company_period");
        builder.HasIndex(x => x.LeaseContractId).HasDatabaseName("idx_payg_lease_usage_lease");
    }
}
