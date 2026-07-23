using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Documents.Entities;

namespace PropertyOS.Infrastructure.Documents.Configurations;

internal sealed class DocumentCategoryConfiguration : IEntityTypeConfiguration<DocumentCategory>
{
    public void Configure(EntityTypeBuilder<DocumentCategory> builder)
    {
        builder.ToTable("document_categories", t =>
        {
            t.HasCheckConstraint("chk_document_categories_name_not_blank", "length(btrim(name)) > 0");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()");

        builder.Property(e => e.CompanyId).HasColumnName("company_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasColumnType("uuid");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasColumnType("uuid");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasColumnType("uuid");

        builder.HasQueryFilter(e => e.DeletedAt == null);

        // Unique Constraint: uq_document_categories_company_name
        builder.HasIndex(e => new { e.CompanyId, e.Name })
            .HasDatabaseName("uq_document_categories_company_name")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // Foreign keys
        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.DeletedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
