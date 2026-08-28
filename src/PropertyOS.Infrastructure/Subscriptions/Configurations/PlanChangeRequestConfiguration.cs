using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Subscriptions;

namespace PropertyOS.Infrastructure.Subscriptions.Configurations;

public sealed class PlanChangeRequestConfiguration : IEntityTypeConfiguration<PlanChangeRequest>
{
    public void Configure(EntityTypeBuilder<PlanChangeRequest> builder)
    {
        builder.ToTable("plan_change_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v7()");
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.SubscriptionId).HasColumnName("subscription_id").IsRequired();
        builder.Property(x => x.CurrentPlanId).HasColumnName("current_plan_id").IsRequired();
        builder.Property(x => x.RequestedPlanId).HasColumnName("requested_plan_id").IsRequired();
        builder.Property(x => x.CurrentBillingCycle).HasColumnName("current_billing_cycle").HasColumnType("billing_cycle_enum").IsRequired();
        builder.Property(x => x.RequestedBillingCycle).HasColumnName("requested_billing_cycle").HasColumnType("billing_cycle_enum").IsRequired();
        builder.Property(x => x.RequestedBy).HasColumnName("requested_by").IsRequired();
        builder.Property(x => x.RequestedAt).HasColumnName("requested_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("plan_change_request_status_enum").HasDefaultValueSql("'pending'::plan_change_request_status_enum").IsRequired();
        builder.Property(x => x.ReviewerId).HasColumnName("reviewer_id");
        builder.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(x => x.DecisionNote).HasColumnName("decision_note").HasMaxLength(1000);
        builder.Property(x => x.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Subscription).WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CurrentPlan).WithMany().HasForeignKey(x => x.CurrentPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RequestedPlan).WithMany().HasForeignKey(x => x.RequestedPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CompanyId)
            .IsUnique()
            .HasDatabaseName("uq_plan_change_requests_one_pending_per_company")
            .HasFilter("status = 'pending'");
        builder.HasIndex(x => new { x.CompanyId, x.RequestedAt })
            .HasDatabaseName("idx_plan_change_requests_company_requested_at");
        builder.HasIndex(x => new { x.Status, x.RequestedAt })
            .HasDatabaseName("idx_plan_change_requests_status_requested_at");
        builder.HasIndex(x => x.SubscriptionId).HasDatabaseName("ix_plan_change_requests_subscription_id");
        builder.HasIndex(x => x.CurrentPlanId).HasDatabaseName("ix_plan_change_requests_current_plan_id");
        builder.HasIndex(x => x.RequestedPlanId).HasDatabaseName("ix_plan_change_requests_requested_plan_id");
        builder.HasIndex(x => x.RequestedBy).HasDatabaseName("ix_plan_change_requests_requested_by");
        builder.HasIndex(x => x.ReviewerId).HasDatabaseName("ix_plan_change_requests_reviewer_id");

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_plan_change_requests_review_state",
            "(status = 'pending' AND reviewer_id IS NULL AND reviewed_at IS NULL AND rejection_reason IS NULL) OR " +
            "(status = 'cancelled' AND reviewer_id IS NULL AND reviewed_at IS NULL AND rejection_reason IS NULL) OR " +
            "(status = 'approved' AND reviewer_id IS NOT NULL AND reviewed_at IS NOT NULL AND rejection_reason IS NULL) OR " +
            "(status = 'rejected' AND reviewer_id IS NOT NULL AND reviewed_at IS NOT NULL AND rejection_reason IS NOT NULL AND length(btrim(rejection_reason)) > 0)"));

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
    }
}
