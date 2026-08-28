using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class LandlordRegistrationConfiguration : IEntityTypeConfiguration<LandlordRegistration>
{
    public void Configure(EntityTypeBuilder<LandlordRegistration> builder)
    {
        builder.ToTable("landlord_registrations", table =>
        {
            table.HasCheckConstraint(
                "chk_landlord_registrations_review",
                "(status = 'pending' AND reviewed_at IS NULL AND reviewed_by IS NULL AND rejection_reason IS NULL) OR " +
                "(status = 'approved' AND rejection_reason IS NULL AND ((reviewed_at IS NULL AND reviewed_by IS NULL) OR (reviewed_at IS NOT NULL AND reviewed_by IS NOT NULL))) OR " +
                "(status = 'rejected' AND reviewed_at IS NOT NULL AND reviewed_by IS NOT NULL AND rejection_reason IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        builder.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.MembershipId).HasColumnName("membership_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("registration_approval_status_enum").IsRequired();
        builder.Property(x => x.RejectionReason).HasColumnName("rejection_reason").HasColumnType("character varying(500)").HasMaxLength(500);
        builder.Property(x => x.SubmittedAt).HasColumnName("submitted_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReviewedBy).HasColumnName("reviewed_by").HasColumnType("uuid");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("uq_landlord_registrations_user_id");
        builder.HasIndex(x => x.CompanyId).IsUnique().HasDatabaseName("uq_landlord_registrations_company_id");
        builder.HasIndex(x => x.MembershipId).IsUnique().HasDatabaseName("uq_landlord_registrations_membership_id");
        builder.HasIndex(x => new { x.Status, x.SubmittedAt }).HasDatabaseName("idx_landlord_registrations_status_submitted_at");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Membership).WithMany().HasForeignKey(x => x.MembershipId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.Restrict);

        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
    }
}
