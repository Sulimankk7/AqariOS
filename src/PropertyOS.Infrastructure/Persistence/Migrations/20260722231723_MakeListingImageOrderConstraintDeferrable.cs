using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeListingImageOrderConstraintDeferrable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE EXTENSION IF NOT EXISTS btree_gist;

                DROP INDEX uq_listing_images_listing_display_order;

                ALTER TABLE listing_images
                    ADD CONSTRAINT uq_listing_images_listing_display_order
                    EXCLUDE USING gist (listing_id WITH =, display_order WITH =)
                    WHERE (deleted_at IS NULL)
                    DEFERRABLE INITIALLY DEFERRED;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE listing_images
                    DROP CONSTRAINT uq_listing_images_listing_display_order;

                CREATE UNIQUE INDEX uq_listing_images_listing_display_order
                    ON listing_images (listing_id, display_order)
                    WHERE (deleted_at IS NULL);
            ");
        }
    }
}
