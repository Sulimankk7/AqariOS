using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Common.Enums;
using PropertyOS.Domain.Common.ValueObjects;
using PropertyOS.Domain.Marketplace;
using PropertyOS.Domain.Marketplace.Enums;

namespace PropertyOS.Infrastructure.Marketplace.Configurations;

internal sealed class MarketplaceListingConfiguration : IEntityTypeConfiguration<MarketplaceListing>
{
    public void Configure(EntityTypeBuilder<MarketplaceListing> builder)
    {
        builder.ToTable("marketplace_listings", t =>
        {
            // Business Check Constraints
            t.HasCheckConstraint(
                "chk_marketplace_listings_monthly_rent_positive",
                "monthly_rent > 0");

            t.HasCheckConstraint(
                "chk_marketplace_listings_security_deposit_nonneg",
                "security_deposit IS NULL OR security_deposit >= 0");

            t.HasCheckConstraint(
                "chk_marketplace_listings_title_not_blank",
                "length(btrim(listing_title)) > 0");

            t.HasCheckConstraint(
                "chk_marketplace_listings_published_date_status_consistency",
                "(status = 'draft' AND published_date IS NULL) OR (status != 'draft' AND published_date IS NOT NULL)");

            t.HasCheckConstraint(
                "chk_marketplace_listings_published_date_not_future",
                "published_date IS NULL OR published_date <= CURRENT_DATE");

            t.HasCheckConstraint(
                "chk_marketplace_listings_expiration_after_published",
                "expiration_date IS NULL OR published_date IS NULL OR expiration_date > published_date");

            t.HasCheckConstraint(
                "chk_marketplace_listings_contact_phone_not_blank",
                "length(btrim(contact_phone)) > 0");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(l => l.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(l => l.BuildingId)
            .HasColumnName("building_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(l => l.ApartmentId)
            .HasColumnName("apartment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(l => l.ListingTitle)
            .HasColumnName("listing_title")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(l => l.ListingDescription)
            .HasColumnName("listing_description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(l => l.MonthlyRent)
            .HasColumnName("monthly_rent")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(l => l.SecurityDeposit)
            .HasColumnName("security_deposit")
            .HasColumnType("numeric(12,3)")
            .IsRequired(false);

        builder.Property(l => l.Currency)
            .HasColumnName("currency")
            .HasColumnType("currency_code_enum")
            .HasDefaultValueSql("'jod'")
            .IsRequired();

        builder.Property(l => l.Status)
            .HasColumnName("status")
            .HasColumnType("listing_status_enum")
            .HasDefaultValueSql("'draft'")
            .IsRequired();

        builder.Property(l => l.PublishedDate)
            .HasColumnName("published_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(l => l.ExpirationDate)
            .HasColumnName("expiration_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(l => l.IsFeatured)
            .HasColumnName("is_featured")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(l => l.ContactPhone)
            .HasColumnName("contact_phone")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .HasConversion(
                p => p.Value,
                v => new PhoneNumber(v))
            .IsRequired();

        builder.Property(l => l.ContactWhatsapp)
            .HasColumnName("contact_whatsapp")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .HasConversion(
                p => p != null ? p.Value : null,
                v => v != null ? new PhoneNumber(v) : null)
            .IsRequired(false);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(l => l.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(l => l.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(l => l.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(l => l.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // ── Foreign Keys ──────────────────────────────────────────────────────

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(l => l.CompanyId)
            .HasConstraintName("fk_marketplace_listings_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Properties.Building>()
            .WithMany()
            .HasForeignKey(l => l.BuildingId)
            .HasConstraintName("fk_marketplace_listings_buildings_building_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Properties.Apartment>()
            .WithMany()
            .HasForeignKey(l => l.ApartmentId)
            .HasConstraintName("fk_marketplace_listings_apartments_apartment_id")
            .OnDelete(DeleteBehavior.Restrict);

        // ── Navigation for child collections ─────────────────────────────────

        builder.HasMany(l => l.Images)
            .WithOne()
            .HasForeignKey(img => img.ListingId)
            .HasConstraintName("fk_listing_images_listing_id")
            .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes (spec §9.1) ──────────────────────────────────────────────

        // Unique listing constraint: At most one published listing per apartment
        builder.HasIndex(l => l.ApartmentId)
            .IsUnique()
            .HasDatabaseName("uq_marketplace_listings_one_active_per_apartment")
            .HasFilter("status = 'published' AND deleted_at IS NULL");

        // Public browse index (featured first, then recency)
        builder.HasIndex(l => new { l.IsFeatured, l.PublishedDate, l.Id })
            .IsDescending(true, true, false)
            .HasDatabaseName("idx_marketplace_listings_public_browse")
            .HasFilter("status = 'published' AND deleted_at IS NULL");

        // Staff dashboard worklist index
        builder.HasIndex(l => new { l.CompanyId, l.Status, l.PublishedDate })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_marketplace_listings_company_status_published")
            .HasFilter("deleted_at IS NULL");

        // GIN Trigram index for title search
        builder.HasIndex(l => l.ListingTitle)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("idx_marketplace_listings_title_trgm");

        // ── Global query filter (soft delete) ────────────────────────────────
        builder.HasQueryFilter(l => l.DeletedAt == null);
    }
}
