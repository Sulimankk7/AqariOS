using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Infrastructure.Notifications.Configurations;

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates", t =>
        {
            t.HasCheckConstraint("chk_notification_templates_name_not_blank", "length(btrim(template_name)) > 0");
            t.HasCheckConstraint("chk_notification_templates_subject_not_blank", "length(btrim(subject)) > 0");
            t.HasCheckConstraint("chk_notification_templates_body_not_blank", "length(btrim(body)) > 0");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(t => t.TemplateName)
            .HasColumnName("template_name")
            .HasColumnType("character varying(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(t => t.NotificationType)
            .HasColumnName("notification_type")
            .HasColumnType("notification_type_enum")
            .IsRequired();

        builder.Property(t => t.Subject)
            .HasColumnName("subject")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.Body)
            .HasColumnName("body")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(t => t.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(t => t.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(t => t.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.HasIndex(t => new { t.CompanyId, t.TemplateName })
            .IsUnique()
            .HasDatabaseName("uq_notification_templates_company_name")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(t => new { t.CompanyId, t.NotificationType })
            .HasDatabaseName("idx_notification_templates_company_type")
            .HasFilter("deleted_at IS NULL AND is_active = true");

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .HasConstraintName("fk_notification_templates_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.CreatedBy)
            .HasConstraintName("fk_notification_templates_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.UpdatedBy)
            .HasConstraintName("fk_notification_templates_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.DeletedBy)
            .HasConstraintName("fk_notification_templates_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(t => t.DeletedAt == null);
    }
}
