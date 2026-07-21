using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Marketplace;

namespace PropertyOS.Infrastructure.Marketplace.Configurations;

internal sealed class ListingImageConfiguration : IEntityTypeConfiguration<ListingImage>
{
    public void Configure(EntityTypeBuilder<ListingImage> builder)
    {
        builder.ToTable("listing_images", t =>
        {
            t.HasCheckConstraint(
                "chk_listing_images_display_order_nonneg",
                "display_order >= 0");
        });

        builder.HasKey(img => img.Id);

        builder.Property(img => img.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(img => img.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(img => img.ListingId)
            .HasColumnName("listing_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(img => img.FileId)
            .HasColumnName("file_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(img => img.DisplayOrder)
            .HasColumnName("display_order")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(img => img.IsCover)
            .HasColumnName("is_cover")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(img => img.UploadedBy)
            .HasColumnName("uploaded_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(img => img.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(img => img.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(img => img.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(img => img.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(img => img.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(img => img.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // ── Foreign Keys ──────────────────────────────────────────────────────

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(img => img.CompanyId)
            .HasConstraintName("fk_listing_images_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Note: No file_storage FK constraint yet since that table is deferred.
        // It will be added in Module 10 migrations.

        // ── Indexes (spec §9.2) ──────────────────────────────────────────────

        // Unique display order per listing
        builder.HasIndex(img => new { img.ListingId, img.DisplayOrder })
            .IsUnique()
            .HasDatabaseName("uq_listing_images_listing_display_order")
            .HasFilter("deleted_at IS NULL");

        // Exactly one cover image per listing
        builder.HasIndex(img => img.ListingId)
            .IsUnique()
            .HasDatabaseName("uq_listing_images_one_cover")
            .HasFilter("is_cover = true AND deleted_at IS NULL");

        // ── Global query filter (soft delete) ────────────────────────────────
        builder.HasQueryFilter(img => img.DeletedAt == null);
    }
}
