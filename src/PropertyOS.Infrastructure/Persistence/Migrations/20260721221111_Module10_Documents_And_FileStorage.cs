using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module10_Documents_And_FileStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_categories", x => x.id);
                    table.CheckConstraint("chk_document_categories_name_not_blank", "length(btrim(name)) > 0");
                    table.ForeignKey(
                        name: "FK_document_categories_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_categories_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_document_categories_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_document_categories_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "file_storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    original_filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_storage", x => x.id);
                    table.ForeignKey(
                        name: "FK_file_storage_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_file_storage_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_file_storage_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_file_storage_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_file_storage_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "building_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_confidential = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_building_documents", x => x.id);
                    table.CheckConstraint("chk_building_documents_expiry_after_issue", "expiry_date IS NULL OR issue_date IS NULL OR expiry_date > issue_date");
                    table.CheckConstraint("chk_building_documents_issue_date_not_future", "issue_date IS NULL OR issue_date <= CURRENT_DATE");
                    table.CheckConstraint("chk_building_documents_name_not_blank", "length(btrim(document_name)) > 0");
                    table.ForeignKey(
                        name: "FK_building_documents_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_building_documents_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_building_documents_document_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "document_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_building_documents_file_storage_file_id",
                        column: x => x.file_id,
                        principalTable: "file_storage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_building_documents_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_building_documents_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_building_documents_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_building_documents_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_attachments_file_id",
                table: "maintenance_request_attachments",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_listing_images_file_id",
                table: "listing_images",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_receipts_file_id",
                table: "expense_receipts",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_documents_file_id",
                table: "contract_documents",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "idx_building_documents_building_category",
                table: "building_documents",
                columns: new[] { "building_id", "category_id" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_building_documents_building_created",
                table: "building_documents",
                columns: new[] { "building_id", "created_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_building_documents_company_category",
                table: "building_documents",
                columns: new[] { "company_id", "category_id" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_building_documents_document_name_trgm",
                table: "building_documents",
                column: "document_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "idx_building_documents_expiring",
                table: "building_documents",
                columns: new[] { "company_id", "expiry_date" },
                filter: "expiry_date IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_building_documents_category_id",
                table: "building_documents",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_building_documents_created_by",
                table: "building_documents",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_building_documents_deleted_by",
                table: "building_documents",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_building_documents_file_id",
                table: "building_documents",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_building_documents_updated_by",
                table: "building_documents",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_building_documents_uploaded_by",
                table: "building_documents",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "uq_building_documents_building_file",
                table: "building_documents",
                columns: new[] { "building_id", "file_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_document_categories_created_by",
                table: "document_categories",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_document_categories_deleted_by",
                table: "document_categories",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_document_categories_updated_by",
                table: "document_categories",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_document_categories_company_name",
                table: "document_categories",
                columns: new[] { "company_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_file_storage_company_id",
                table: "file_storage",
                column: "company_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_file_storage_created_by",
                table: "file_storage",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_file_storage_deleted_by",
                table: "file_storage",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_file_storage_updated_by",
                table: "file_storage",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_file_storage_uploaded_by",
                table: "file_storage",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "uq_file_storage_storage_key",
                table: "file_storage",
                column: "storage_key",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_contract_documents_file_storage_file_id",
                table: "contract_documents",
                column: "file_id",
                principalTable: "file_storage",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expense_receipts_file_storage_file_id",
                table: "expense_receipts",
                column: "file_id",
                principalTable: "file_storage",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_listing_images_file_storage_file_id",
                table: "listing_images",
                column: "file_id",
                principalTable: "file_storage",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_maintenance_request_attachments_file_storage_file_id",
                table: "maintenance_request_attachments",
                column: "file_id",
                principalTable: "file_storage",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                GRANT SELECT, INSERT, UPDATE, DELETE ON document_categories, file_storage, building_documents TO propertyos_app;

                -- Enable Row Level Security (RLS) on all Module 10 tables
                ALTER TABLE file_storage ENABLE ROW LEVEL SECURITY;
                ALTER TABLE file_storage FORCE ROW LEVEL SECURITY;
                CREATE POLICY file_storage_tenant_isolation_policy ON file_storage
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE document_categories ENABLE ROW LEVEL SECURITY;
                ALTER TABLE document_categories FORCE ROW LEVEL SECURITY;
                CREATE POLICY document_categories_tenant_isolation_policy ON document_categories
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE building_documents ENABLE ROW LEVEL SECURITY;
                ALTER TABLE building_documents FORCE ROW LEVEL SECURITY;
                CREATE POLICY building_documents_tenant_isolation_policy ON building_documents
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS building_documents_tenant_isolation_policy ON building_documents;
                ALTER TABLE building_documents DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS document_categories_tenant_isolation_policy ON document_categories;
                ALTER TABLE document_categories DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS file_storage_tenant_isolation_policy ON file_storage;
                ALTER TABLE file_storage DISABLE ROW LEVEL SECURITY;
            ");

            migrationBuilder.DropForeignKey(
                name: "fk_contract_documents_file_storage_file_id",
                table: "contract_documents");

            migrationBuilder.DropForeignKey(
                name: "fk_expense_receipts_file_storage_file_id",
                table: "expense_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_listing_images_file_storage_file_id",
                table: "listing_images");

            migrationBuilder.DropForeignKey(
                name: "fk_maintenance_request_attachments_file_storage_file_id",
                table: "maintenance_request_attachments");

            migrationBuilder.DropTable(
                name: "building_documents");

            migrationBuilder.DropTable(
                name: "document_categories");

            migrationBuilder.DropTable(
                name: "file_storage");

            migrationBuilder.DropIndex(
                name: "IX_maintenance_request_attachments_file_id",
                table: "maintenance_request_attachments");

            migrationBuilder.DropIndex(
                name: "IX_listing_images_file_id",
                table: "listing_images");

            migrationBuilder.DropIndex(
                name: "IX_expense_receipts_file_id",
                table: "expense_receipts");

            migrationBuilder.DropIndex(
                name: "IX_contract_documents_file_id",
                table: "contract_documents");
        }
    }
}
