using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Subscriptions;

namespace PropertyOS.Infrastructure.Subscriptions.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v7()");

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.NameEn)
            .HasColumnName("name_en")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.NameAr)
            .HasColumnName("name_ar")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DescriptionEn)
            .HasColumnName("description_en");

        builder.Property(x => x.DescriptionAr)
            .HasColumnName("description_ar");

        builder.Property(x => x.MonthlyPrice)
            .HasColumnName("monthly_price")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(x => x.YearlyPrice)
            .HasColumnName("yearly_price")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasColumnType("char(3)")
            .HasDefaultValue("JOD")
            .IsRequired();

        builder.Property(x => x.MaxBuildings)
            .HasColumnName("max_buildings");

        builder.Property(x => x.MaxUsers)
            .HasColumnName("max_users");

        builder.Property(x => x.MaxStorageMb)
            .HasColumnName("max_storage_mb");

        builder.Property(x => x.FeatureFlags)
            .HasColumnName("feature_flags")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired();

        builder.Property(x => x.SupportsTrial)
            .HasColumnName("supports_trial")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.TrialDurationDays)
            .HasColumnName("trial_duration_days");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        // Unique constraints
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("uq_subscription_plans_code");

        // Indexes
        builder.HasIndex(x => new { x.IsActive, x.SortOrder })
            .HasDatabaseName("idx_subscription_plans_active_sort")
            .HasFilter("is_active = true");

        // Check Constraints
        builder.ToTable(t => 
        {
            t.HasCheckConstraint("chk_subscription_plans_trial_duration", "(supports_trial = true AND trial_duration_days > 0) OR (supports_trial = false AND trial_duration_days IS NULL)");
            t.HasCheckConstraint("chk_subscription_plans_prices_positive", "monthly_price > 0 AND yearly_price > 0");
            t.HasCheckConstraint("chk_subscription_plans_quotas_positive", "(max_buildings IS NULL OR max_buildings > 0) AND (max_users IS NULL OR max_users > 0) AND (max_storage_mb IS NULL OR max_storage_mb > 0)");
            t.HasCheckConstraint("chk_subscription_plans_sort_order", "sort_order >= 0");
        });

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
    }
}
