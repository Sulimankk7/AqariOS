using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module5_Leasing_Tenant_Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    national_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occupation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    employer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                    table.UniqueConstraint("uq_tenants_company_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "fk_tenants_companies",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenants_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenants_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenants_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenants_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tenant_emergency_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    relationship_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_emergency_contacts", x => x.id);
                    table.CheckConstraint("chk_tenant_emergency_contacts_relationship", "btrim(relationship_type) <> ''");
                    table.ForeignKey(
                        name: "fk_tenant_emergency_contacts_tenants_company_tenant",
                        columns: x => new { x.company_id, x.tenant_id },
                        principalTable: "tenants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_emergency_contacts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenant_emergency_contacts_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenant_emergency_contacts_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tenant_family_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    relationship_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    age_bracket = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_family_members", x => x.id);
                    table.CheckConstraint("chk_tenant_family_members_age_bracket", "age_bracket IS NULL OR btrim(age_bracket) <> ''");
                    table.CheckConstraint("chk_tenant_family_members_relationship", "btrim(relationship_type) <> ''");
                    table.ForeignKey(
                        name: "fk_tenant_family_members_tenants_company_tenant",
                        columns: x => new { x.company_id, x.tenant_id },
                        principalTable: "tenants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_family_members_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenant_family_members_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenant_family_members_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tenant_vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    make_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_vehicles_tenants_company_tenant",
                        columns: x => new { x.company_id, x.tenant_id },
                        principalTable: "tenants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_vehicles_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenant_vehicles_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenant_vehicles_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_tenant_emergency_contacts_tenant",
                table: "tenant_emergency_contacts",
                columns: new[] { "company_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_emergency_contacts_created_by",
                table: "tenant_emergency_contacts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_emergency_contacts_deleted_by",
                table: "tenant_emergency_contacts",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_emergency_contacts_updated_by",
                table: "tenant_emergency_contacts",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_tenant_family_members_tenant",
                table: "tenant_family_members",
                columns: new[] { "company_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_family_members_created_by",
                table: "tenant_family_members",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_family_members_deleted_by",
                table: "tenant_family_members",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_family_members_updated_by",
                table: "tenant_family_members",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_tenant_vehicles_tenant",
                table: "tenant_vehicles",
                columns: new[] { "company_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_vehicles_created_by",
                table: "tenant_vehicles",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_vehicles_deleted_by",
                table: "tenant_vehicles",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_vehicles_updated_by",
                table: "tenant_vehicles",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_vehicles_company_plate",
                table: "tenant_vehicles",
                columns: new[] { "company_id", "plate_number" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_tenants_company_phone",
                table: "tenants",
                columns: new[] { "company_id", "phone" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_created_by",
                table: "tenants",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_deleted_by",
                table: "tenants",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_updated_by",
                table: "tenants",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_user_id",
                table: "tenants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenants_company_national_id",
                table: "tenants",
                columns: new[] { "company_id", "national_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.Sql(@"
                ALTER TABLE tenants OWNER TO propertyos_owner;
                ALTER TABLE tenant_family_members OWNER TO propertyos_owner;
                ALTER TABLE tenant_emergency_contacts OWNER TO propertyos_owner;
                ALTER TABLE tenant_vehicles OWNER TO propertyos_owner;

                ALTER TABLE tenants ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenants FORCE ROW LEVEL SECURITY;
                CREATE POLICY ""Tenant Isolation"" ON tenants FOR ALL TO propertyos_app USING (company_id = current_setting('app.current_company_id')::uuid) WITH CHECK (company_id = current_setting('app.current_company_id')::uuid);

                ALTER TABLE tenant_family_members ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_family_members FORCE ROW LEVEL SECURITY;
                CREATE POLICY ""Tenant Isolation"" ON tenant_family_members FOR ALL TO propertyos_app USING (company_id = current_setting('app.current_company_id')::uuid) WITH CHECK (company_id = current_setting('app.current_company_id')::uuid);

                ALTER TABLE tenant_emergency_contacts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_emergency_contacts FORCE ROW LEVEL SECURITY;
                CREATE POLICY ""Tenant Isolation"" ON tenant_emergency_contacts FOR ALL TO propertyos_app USING (company_id = current_setting('app.current_company_id')::uuid) WITH CHECK (company_id = current_setting('app.current_company_id')::uuid);

                ALTER TABLE tenant_vehicles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE tenant_vehicles FORCE ROW LEVEL SECURITY;
                CREATE POLICY ""Tenant Isolation"" ON tenant_vehicles FOR ALL TO propertyos_app USING (company_id = current_setting('app.current_company_id')::uuid) WITH CHECK (company_id = current_setting('app.current_company_id')::uuid);

                GRANT SELECT, INSERT, UPDATE ON tenants TO propertyos_app;
                REVOKE DELETE ON tenants FROM propertyos_app;
                REVOKE ALL ON tenants FROM propertyos_auth;

                GRANT SELECT, INSERT, UPDATE ON tenant_family_members TO propertyos_app;
                REVOKE DELETE ON tenant_family_members FROM propertyos_app;
                REVOKE ALL ON tenant_family_members FROM propertyos_auth;

                GRANT SELECT, INSERT, UPDATE ON tenant_emergency_contacts TO propertyos_app;
                REVOKE DELETE ON tenant_emergency_contacts FROM propertyos_app;
                REVOKE ALL ON tenant_emergency_contacts FROM propertyos_auth;

                GRANT SELECT, INSERT, UPDATE ON tenant_vehicles TO propertyos_app;
                REVOKE DELETE ON tenant_vehicles FROM propertyos_app;
                REVOKE ALL ON tenant_vehicles FROM propertyos_auth;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenant_vehicles;
                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenant_emergency_contacts;
                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenant_family_members;
                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenants;
            ");

            migrationBuilder.DropTable(
                name: "tenant_emergency_contacts");

            migrationBuilder.DropTable(
                name: "tenant_family_members");

            migrationBuilder.DropTable(
                name: "tenant_vehicles");

            migrationBuilder.DropTable(
                name: "tenants");
        }
    }
}
