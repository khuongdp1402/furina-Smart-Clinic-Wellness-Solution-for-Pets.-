using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Furina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVaccinations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vaccination_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vaccine_name = table.Column<string>(type: "text", nullable: false),
                    date_given = table.Column<DateOnly>(type: "date", nullable: false),
                    next_due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vaccination_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_vaccination_records_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vaccination_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vaccination_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    milestone_day = table.Column<int>(type: "integer", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_logs_vaccination_records_vaccination_record_id",
                        column: x => x.vaccination_record_id,
                        principalTable: "vaccination_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_tenant_id",
                table: "notification_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_vaccination_record_id_milestone_day",
                table: "notification_logs",
                columns: new[] { "vaccination_record_id", "milestone_day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vaccination_records_pet_id_next_due_date",
                table: "vaccination_records",
                columns: new[] { "pet_id", "next_due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_vaccination_records_tenant_id",
                table: "vaccination_records",
                column: "tenant_id");

            foreach (var table in new[] { "vaccination_records", "notification_logs" })
            {
                migrationBuilder.Sql($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE {table} FORCE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($@"
                    CREATE POLICY tenant_isolation ON {table}
                    USING (tenant_id = current_setting('app.tenant_id', true)::uuid);");
            }

            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE, DELETE ON vaccination_records, notification_logs TO furina_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "vaccination_records", "notification_logs" })
            {
                migrationBuilder.Sql($"DROP POLICY IF EXISTS tenant_isolation ON {table};");
                migrationBuilder.Sql($"ALTER TABLE {table} DISABLE ROW LEVEL SECURITY;");
            }

            migrationBuilder.DropTable(
                name: "notification_logs");

            migrationBuilder.DropTable(
                name: "vaccination_records");
        }
    }
}
