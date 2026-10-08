using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Furina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "service_catalog_id",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "service_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    default_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    default_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_catalog", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_catalog_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clinic_service_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinic_service_prices", x => x.id);
                    table.ForeignKey(
                        name: "FK_clinic_service_prices_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_clinic_service_prices_service_catalog_service_catalog_id",
                        column: x => x.service_catalog_id,
                        principalTable: "service_catalog",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_clinic_service_prices_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_service_catalog_id",
                table: "appointments",
                column: "service_catalog_id");

            migrationBuilder.CreateIndex(
                name: "IX_clinic_service_prices_clinic_id_service_catalog_id",
                table: "clinic_service_prices",
                columns: new[] { "clinic_id", "service_catalog_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clinic_service_prices_service_catalog_id",
                table: "clinic_service_prices",
                column: "service_catalog_id");

            migrationBuilder.CreateIndex(
                name: "IX_clinic_service_prices_tenant_id",
                table: "clinic_service_prices",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_catalog_tenant_id",
                table: "service_catalog",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_service_catalog_service_catalog_id",
                table: "appointments",
                column: "service_catalog_id",
                principalTable: "service_catalog",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            foreach (var table in new[] { "service_catalog", "clinic_service_prices" })
            {
                migrationBuilder.Sql($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE {table} FORCE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($@"
                    CREATE POLICY tenant_isolation ON {table}
                    USING (tenant_id = current_setting('app.tenant_id', true)::uuid);");
            }

            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE, DELETE ON service_catalog, clinic_service_prices TO furina_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "service_catalog", "clinic_service_prices" })
            {
                migrationBuilder.Sql($"DROP POLICY IF EXISTS tenant_isolation ON {table};");
                migrationBuilder.Sql($"ALTER TABLE {table} DISABLE ROW LEVEL SECURITY;");
            }

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_service_catalog_service_catalog_id",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "clinic_service_prices");

            migrationBuilder.DropTable(
                name: "service_catalog");

            migrationBuilder.DropIndex(
                name: "IX_appointments_service_catalog_id",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "service_catalog_id",
                table: "appointments");
        }
    }
}
