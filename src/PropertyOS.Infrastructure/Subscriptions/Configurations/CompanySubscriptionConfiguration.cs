using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Subscriptions;

namespace PropertyOS.Infrastructure.Subscriptions.Configurations;

public class CompanySubscriptionConfiguration : IEntityTypeConfiguration<CompanySubscription>
{
    public void Configure(EntityTypeBuilder<CompanySubscription> builder)
    {
        builder.ToTable("company_subscriptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v7()");

        builder.Property(x => x.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(x => x.PlanId)
            .HasColumnName("plan_id")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("subscription_status_enum")
            .HasDefaultValueSql("'trialing'::subscription_status_enum")
            .IsRequired();

        builder.Property(x => x.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(x => x.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        builder.Property(x => x.TrialEndDate)
            .HasColumnName("trial_end_date");

        builder.Property(x => x.PriceAtSubscription)
            .HasColumnName("price_at_subscription")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(x => x.CurrencyAtSubscription)
            .HasColumnName("currency_at_subscription")
            .HasColumnType("char(3)")
            .HasDefaultValue("JOD")
            .IsRequired();

        builder.Property(x => x.BillingCycle)
            .HasColumnName("billing_cycle")
            .HasColumnType("billing_cycle_enum")
            .HasDefaultValueSql("'monthly'::billing_cycle_enum")
            .IsRequired();

        builder.Property(x => x.AutoRenew)
            .HasColumnName("auto_renew")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.SuspendedAt)
            .HasColumnName("suspended_at");

        builder.Property(x => x.SuspensionReason)
            .HasColumnName("suspension_reason")
            .HasMaxLength(255);

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(x => x.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(255);

        builder.Property(x => x.ExpiredAt)
            .HasColumnName("expired_at");

        builder.Property(x => x.ExternalBillingRef)
            .HasColumnName("external_billing_ref")
            .HasMaxLength(255);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        // Foreign keys
        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Plan)
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraints
        builder.HasIndex(x => x.CompanyId)
            .IsUnique()
            .HasDatabaseName("uq_company_subscriptions_one_active")
            .HasFilter("status IN ('trialing','active','past_due')");

        // Indexes
        builder.HasIndex(x => new { x.CompanyId, x.Status })
            .HasDatabaseName("idx_company_subscriptions_company_status");

        builder.HasIndex(x => x.EndDate)
            .HasDatabaseName("idx_company_subscriptions_end_date")
            .HasFilter("status IN ('trialing','active','past_due')");

        // Check Constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_company_subscriptions_dates", "end_date > start_date");
            t.HasCheckConstraint("chk_company_subscriptions_trial_end", "trial_end_date IS NOT NULL OR status != 'trialing'");
            t.HasCheckConstraint("chk_company_subscriptions_suspension", "suspension_reason IS NOT NULL OR suspended_at IS NULL");
        });

        // Concurrency token
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
    }
}
