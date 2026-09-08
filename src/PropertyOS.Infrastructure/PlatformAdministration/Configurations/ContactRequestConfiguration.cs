using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.PlatformAdministration;

namespace PropertyOS.Infrastructure.PlatformAdministration.Configurations;

internal sealed class ContactRequestConfiguration : IEntityTypeConfiguration<ContactRequest>
{
    public void Configure(EntityTypeBuilder<ContactRequest> builder)
    {
        builder.ToTable("contact_requests", table => table.HasCheckConstraint("chk_contact_requests_number_of_buildings", "number_of_buildings BETWEEN 1 AND 10000"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasColumnName("name").HasColumnType("character varying(120)").HasMaxLength(120).IsRequired();
        builder.Property(x => x.CompanyName).HasColumnName("company_name").HasColumnType("character varying(160)").HasMaxLength(160).IsRequired();
        builder.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasColumnType("character varying(24)").HasMaxLength(24).IsRequired();
        builder.Property(x => x.NumberOfBuildings).HasColumnName("number_of_buildings").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes").HasColumnType("character varying(1000)").HasMaxLength(1000);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasColumnType("character varying(32)").HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("idx_contact_requests_status_created_at");
        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
    }
}
