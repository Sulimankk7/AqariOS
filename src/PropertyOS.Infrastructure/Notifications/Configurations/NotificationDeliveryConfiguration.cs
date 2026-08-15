using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Infrastructure.Notifications.Configurations;

internal sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries", t =>
        {
            t.HasCheckConstraint("chk_notification_deliveries_attempt_count_nonneg", "attempt_count >= 0");
            t.HasCheckConstraint("chk_notification_deliveries_sent_requires_attempt", "delivery_status = 'pending' OR attempt_count > 0");

            t.HasCheckConstraint("chk_notification_deliveries_sent_at_requires_status", "(delivery_status = 'pending' AND sent_at IS NULL) OR (delivery_status IN ('sent', 'delivered', 'failed') AND sent_at IS NOT NULL)");
            
            t.HasCheckConstraint("chk_notification_deliveries_delivered_requires_delivered_at", "delivery_status != 'delivered' OR delivered_at IS NOT NULL");
            t.HasCheckConstraint("chk_notification_deliveries_delivered_after_sent", "delivered_at IS NULL OR sent_at IS NULL OR delivered_at >= sent_at");
            t.HasCheckConstraint("chk_notification_deliveries_failed_requires_reason", "delivery_status != 'failed' OR failure_reason IS NOT NULL");
        });

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(d => d.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(d => d.NotificationId)
            .HasColumnName("notification_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(d => d.DeliveryChannel)
            .HasColumnName("delivery_channel")
            .HasColumnType("delivery_channel_enum")
            .IsRequired();

        builder.Property(d => d.DeliveryStatus)
            .HasColumnName("delivery_status")
            .HasColumnType("delivery_status_enum")
            .HasDefaultValueSql("'pending'")
            .IsRequired();

        builder.Property(d => d.AttemptCount)
            .HasColumnName("attempt_count")
            .HasColumnType("smallint")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(d => d.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(d => d.DeliveredAt)
            .HasColumnName("delivered_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(d => d.FailureReason)
            .HasColumnName("failure_reason")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(d => d.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(d => d.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // No Soft Delete columns on this table

        builder.HasIndex(d => new { d.NotificationId, d.DeliveryChannel })
            .IsUnique()
            .HasDatabaseName("uq_notification_deliveries_notification_channel");

        builder.HasIndex(d => new { d.CompanyId, d.DeliveryStatus, d.SentAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_notification_deliveries_company_status_sent_at")
            .HasFilter("delivery_status = 'failed'");

        builder.HasIndex(d => new { d.CompanyId, d.DeliveryChannel, d.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_notification_deliveries_company_channel_created");

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(d => d.CompanyId)
            .HasConstraintName("fk_notification_deliveries_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(d => d.CreatedBy)
            .HasConstraintName("fk_notification_deliveries_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(d => d.UpdatedBy)
            .HasConstraintName("fk_notification_deliveries_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
