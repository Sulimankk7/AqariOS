using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Common.ValueObjects;
using PropertyOS.Domain.Marketplace;
using PropertyOS.Domain.Marketplace.Enums;

namespace PropertyOS.Infrastructure.Marketplace.Configurations;

internal sealed class ViewingRequestConfiguration : IEntityTypeConfiguration<ViewingRequest>
{
    public void Configure(EntityTypeBuilder<ViewingRequest> builder)
    {
        builder.ToTable("viewing_requests", t =>
        {
            t.HasCheckConstraint(
                "chk_viewing_requests_applicant_name_not_blank",
                "length(btrim(applicant_name)) > 0");

            t.HasCheckConstraint(
                "chk_viewing_requests_phone_not_blank",
                "length(btrim(phone_number)) > 0");

            t.HasCheckConstraint(
                "chk_viewing_requests_preferred_date_not_before_submission",
                "preferred_viewing_date IS NULL OR preferred_viewing_date >= (submitted_at AT TIME ZONE 'UTC')::date");
        });

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(v => v.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(v => v.ListingId)
            .HasColumnName("listing_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(v => v.ApplicantName)
            .HasColumnName("applicant_name")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.PhoneNumber)
            .HasColumnName("phone_number")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .HasConversion(
                p => p.Value,
                v => new PhoneNumber(v))
            .IsRequired();

        builder.Property(v => v.Email)
            .HasColumnName("email")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(v => v.PreferredViewingDate)
            .HasColumnName("preferred_viewing_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(v => v.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(v => v.StaffNotes)
            .HasColumnName("staff_notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(v => v.RequestStatus)
            .HasColumnName("request_status")
            .HasColumnType("viewing_request_status_enum")
            .HasDefaultValueSql("'pending'")
            .IsRequired();

        builder.Property(v => v.SubmittedAt)
            .HasColumnName("submitted_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(v => v.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(v => v.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(v => v.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(v => v.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(v => v.DeletedBy)
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
            .HasForeignKey(v => v.CompanyId)
            .HasConstraintName("fk_viewing_requests_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MarketplaceListing>()
            .WithMany()
            .HasForeignKey(v => v.ListingId)
            .HasConstraintName("fk_viewing_requests_listings_listing_id")
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes (spec §9.3) ──────────────────────────────────────────────

        // Company worklist sorted by recency
        builder.HasIndex(v => new { v.CompanyId, v.RequestStatus, v.SubmittedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_viewing_requests_company_status_submitted")
            .HasFilter("deleted_at IS NULL");

        // Listing detail page viewing requests
        builder.HasIndex(v => new { v.ListingId, v.SubmittedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_viewing_requests_listing_submitted")
            .HasFilter("deleted_at IS NULL");

        // ── Global query filter (soft delete) ────────────────────────────────
        builder.HasQueryFilter(v => v.DeletedAt == null);
    }
}
