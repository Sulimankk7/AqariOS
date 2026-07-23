using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Files.Entities;

namespace PropertyOS.Infrastructure.Files.Configurations;

internal sealed class FileStorageConfiguration : IEntityTypeConfiguration<FileStorage>
{
    public void Configure(EntityTypeBuilder<FileStorage> builder)
    {
        builder.ToTable("file_storage");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()");

        builder.Property(e => e.CompanyId).HasColumnName("company_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.UploadedBy).HasColumnName("uploaded_by").HasColumnType("uuid");

        builder.Property(e => e.OriginalFilename).HasColumnName("original_filename").HasMaxLength(255).IsRequired();
        builder.Property(e => e.MimeType).HasColumnName("mime_type").HasMaxLength(100).IsRequired();
        builder.Property(e => e.SizeBytes).HasColumnName("size_bytes").HasColumnType("bigint").IsRequired();
        builder.Property(e => e.StorageKey).HasColumnName("storage_key").HasMaxLength(500).IsRequired();

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasColumnType("uuid");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasColumnType("uuid");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasColumnType("uuid");

        builder.HasQueryFilter(e => e.DeletedAt == null);

        // Indexes & Constraints
        builder.HasIndex(e => e.CompanyId).HasDatabaseName("idx_file_storage_company_id").HasFilter("deleted_at IS NULL");
        builder.HasIndex(e => e.StorageKey).HasDatabaseName("uq_file_storage_storage_key").IsUnique().HasFilter("deleted_at IS NULL");

        // Foreign keys
        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.UploadedBy)
            .OnDelete(DeleteBehavior.SetNull);

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
