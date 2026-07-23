using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Infrastructure.Notifications.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", t =>
        {
            t.HasCheckConstraint("chk_notifications_subject_not_blank", "length(btrim(subject)) > 0");
            t.HasCheckConstraint("chk_notifications_body_not_blank", "length(btrim(body)) > 0");
            t.HasCheckConstraint("chk_notifications_read_at_not_future", "read_at IS NULL OR read_at <= now()");
            t.HasCheckConstraint("chk_notifications_read_at_after_created", "read_at IS NULL OR read_at >= created_at");
            t.HasCheckConstraint("chk_notifications_read_requires_sent", "read_at IS NULL OR status = 'sent'");
        });

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(n => n.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(n => n.RecipientUserId)
            .HasColumnName("recipient_user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(n => n.TemplateId)
            .HasColumnName("template_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(n => n.NotificationType)
            .HasColumnName("notification_type")
            .HasColumnType("notification_type_enum")
            .IsRequired();

        builder.Property(n => n.Subject)
            .HasColumnName("subject")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(n => n.Body)
            .HasColumnName("body")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasColumnType("notification_status_enum")
            .HasDefaultValueSql("'pending'")
            .IsRequired();

        builder.Property(n => n.Priority)
            .HasColumnName("priority")
            .HasColumnType("notification_priority_enum")
            .HasDefaultValueSql("'normal'")
            .IsRequired();

        builder.Property(n => n.ReadAt)
            .HasColumnName("read_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(n => n.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(n => n.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(n => n.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(n => n.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_notifications_recipient_created")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_notifications_recipient_unread")
            .HasFilter("read_at IS NULL AND status = 'sent' AND deleted_at IS NULL");

        builder.HasIndex(n => new { n.CompanyId, n.Status, n.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_notifications_company_status_created")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(n => new { n.CompanyId, n.NotificationType, n.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_notifications_company_type_created")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(n => n.Subject)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("idx_notifications_subject_trgm");

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(n => n.CompanyId)
            .HasConstraintName("fk_notifications_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .HasConstraintName("fk_notifications_users_recipient_user_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Notifications.NotificationTemplate>()
            .WithMany()
            .HasForeignKey(n => n.TemplateId)
            .HasConstraintName("fk_notifications_notification_templates_template_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(n => n.CreatedBy)
            .HasConstraintName("fk_notifications_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(n => n.UpdatedBy)
            .HasConstraintName("fk_notifications_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(n => n.DeletedBy)
            .HasConstraintName("fk_notifications_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(n => n.Deliveries)
            .WithOne()
            .HasForeignKey(d => d.NotificationId)
            .HasConstraintName("fk_notification_deliveries_notifications_notification_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(n => n.DeletedAt == null);
    }
}
