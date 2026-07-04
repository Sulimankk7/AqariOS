using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Audit.Entities;
using PropertyOS.Domain.Audit.Enums;

namespace PropertyOS.Infrastructure.Audit.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        
        builder.Property(e => e.ActorUserId).HasColumnName("actor_user_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.CompanyId).HasColumnName("company_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.EntityName).HasColumnName("entity_name").HasColumnType("character varying(100)").HasMaxLength(100).IsRequired();
        builder.Property(e => e.EntityId).HasColumnName("entity_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.Action).HasColumnName("action").HasColumnType("audit_action_enum").IsRequired();
        builder.Property(e => e.PreviousValues).HasColumnName("previous_values").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.NewValues).HasColumnName("new_values").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasColumnType("inet").IsRequired(false);
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.RequestId).HasColumnName("request_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(e => e.Severity).HasColumnName("severity").HasColumnType("audit_severity_enum").HasDefaultValue(AuditSeverity.Info).IsRequired();
        builder.Property(e => e.Source).HasColumnName("source").HasColumnType("audit_source_enum").HasDefaultValue(AuditSource.Api).IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(e => new { e.CompanyId, e.OccurredAt }).HasDatabaseName("idx_audit_logs_company_occurred_at"); 
        builder.HasIndex(e => new { e.EntityName, e.EntityId, e.OccurredAt }).HasDatabaseName("idx_audit_logs_entity"); 
        builder.HasIndex(e => new { e.ActorUserId, e.OccurredAt }).HasDatabaseName("idx_audit_logs_actor_user_id"); 

        builder.HasOne(e => e.ActorUser).WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.SetNull);
    }
}
