using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Subscriptions;

namespace PropertyOS.Infrastructure.Subscriptions.Configurations;

internal sealed class PaygUsagePeriodConfiguration : IEntityTypeConfiguration<PaygUsagePeriod>
{
    public void Configure(EntityTypeBuilder<PaygUsagePeriod> builder)
    {
        builder.ToTable("payg_usage_periods", table =>
        {
            table.HasCheckConstraint("chk_payg_usage_period_dates", "period_end > period_start AND calculated_through >= period_start AND calculated_through <= period_end");
            table.HasCheckConstraint("chk_payg_usage_period_price", "monthly_equivalent_unit_price_snapshot > 0");
            table.HasCheckConstraint("chk_payg_usage_period_totals", "period_day_count > 0 AND active_lease_count >= 0 AND accumulated_lease_days >= 0 AND estimated_amount >= 0 AND projected_amount >= 0");
            table.HasCheckConstraint("chk_payg_usage_period_finalized", "(is_finalized = false AND finalized_at IS NULL) OR (is_finalized = true AND finalized_at IS NOT NULL AND calculated_through = period_end)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.CompanySubscriptionId).HasColumnName("company_subscription_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.PeriodStart).HasColumnName("period_start").HasColumnType("date").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnName("period_end").HasColumnType("date").IsRequired();
        builder.Property(x => x.BillingCycle).HasColumnName("billing_cycle").HasColumnType("billing_cycle_enum").IsRequired();
        builder.Property(x => x.MonthlyEquivalentUnitPriceSnapshot).HasColumnName("monthly_equivalent_unit_price_snapshot").HasColumnType("numeric(12,3)").IsRequired();
        builder.Property(x => x.CurrencySnapshot).HasColumnName("currency_snapshot").HasColumnType("char(3)").IsRequired();
        builder.Property(x => x.PeriodDayCount).HasColumnName("period_day_count").IsRequired();
        builder.Property(x => x.ActiveLeaseCount).HasColumnName("active_lease_count").IsRequired();
        builder.Property(x => x.AccumulatedLeaseDays).HasColumnName("accumulated_lease_days").IsRequired();
        builder.Property(x => x.EstimatedAmount).HasColumnName("estimated_amount").HasColumnType("numeric(14,3)").IsRequired();
        builder.Property(x => x.ProjectedAmount).HasColumnName("projected_amount").HasColumnType("numeric(14,3)").IsRequired();
        builder.Property(x => x.CalculatedThrough).HasColumnName("calculated_through").HasColumnType("date").IsRequired();
        builder.Property(x => x.IsChargeable).HasColumnName("is_chargeable").IsRequired();
        builder.Property(x => x.IsFinalized).HasColumnName("is_finalized").IsRequired();
        builder.Property(x => x.FinalizedAt).HasColumnName("finalized_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasAlternateKey(x => new { x.CompanyId, x.Id }).HasName("uq_payg_usage_periods_company_id");
        builder.HasOne<PropertyOS.Domain.Companies.Company>().WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CompanySubscription).WithMany().HasForeignKey(x => x.CompanySubscriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CompanySubscriptionId, x.PeriodStart, x.PeriodEnd })
            .IsUnique().HasDatabaseName("uq_payg_usage_period_subscription_dates");
        builder.HasIndex(x => new { x.CompanyId, x.PeriodStart })
            .IsDescending(false, true).HasDatabaseName("idx_payg_usage_period_company_start");
        builder.HasIndex(x => new { x.IsFinalized, x.PeriodEnd })
            .HasDatabaseName("idx_payg_usage_period_unfinalized_end").HasFilter("is_finalized = false");
    }
}
