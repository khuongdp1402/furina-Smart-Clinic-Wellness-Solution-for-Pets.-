using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Furina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prescriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vet_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    items = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_prescriptions_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prescriptions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prescriptions_visits_visit_id",
                        column: x => x.visit_id,
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_pet_id_created_at",
                table: "prescriptions",
                columns: new[] { "pet_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_tenant_id",
                table: "prescriptions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_visit_id",
                table: "prescriptions",
                column: "visit_id",
                unique: true);

            migrationBuilder.Sql("ALTER TABLE prescriptions ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE prescriptions FORCE ROW LEVEL SECURITY;");
            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation ON prescriptions
                USING (tenant_id = current_setting('app.tenant_id', true)::uuid);");
            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE, DELETE ON prescriptions TO furina_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON prescriptions;");
            migrationBuilder.Sql("ALTER TABLE prescriptions DISABLE ROW LEVEL SECURITY;");

            migrationBuilder.DropTable(
                name: "prescriptions");
        }
    }
}
