using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Infrastructure.Financials.Configurations;

internal sealed class CompanyReceiptSequenceConfiguration : IEntityTypeConfiguration<CompanyReceiptSequence>
{
    public void Configure(EntityTypeBuilder<CompanyReceiptSequence> builder)
    {
        builder.ToTable("company_receipt_sequences", t =>
        {
            t.HasCheckConstraint("chk_company_receipt_sequences_current_number_nonneg", "current_number >= 0");
            t.HasCheckConstraint("chk_company_receipt_sequences_padding_length", "padding_length BETWEEN 1 AND 10");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(s => s.Prefix)
            .HasColumnName("prefix")
            .HasColumnType("character varying(10)")
            .HasMaxLength(10)
            .HasDefaultValueSql("''")
            .IsRequired();

        builder.Property(s => s.CurrentNumber)
            .HasColumnName("current_number")
            .HasColumnType("bigint")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(s => s.PaddingLength)
            .HasColumnName("padding_length")
            .HasColumnType("smallint")
            .HasDefaultValue((short)5)
            .IsRequired();

        builder.Property(s => s.ResetPolicy)
            .HasColumnName("reset_policy")
            .HasColumnType("receipt_reset_policy_enum")
            .HasDefaultValueSql("'never'")
            .IsRequired();

        builder.Property(s => s.LastResetAt)
            .HasColumnName("last_reset_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasAlternateKey(s => s.CompanyId)
            .HasName("uq_company_receipt_sequences_company_id");

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithOne()
            .HasForeignKey<CompanyReceiptSequence>(s => s.CompanyId)
            .HasConstraintName("fk_company_receipt_sequences_companies_company_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
