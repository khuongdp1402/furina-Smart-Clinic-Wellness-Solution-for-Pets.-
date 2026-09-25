using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Furina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentReminderLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appointment_reminder_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_reminder_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_appointment_reminder_logs_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointment_reminder_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointment_reminder_logs_appointment_id",
                table: "appointment_reminder_logs",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointment_reminder_logs_tenant_id",
                table: "appointment_reminder_logs",
                column: "tenant_id");

            migrationBuilder.Sql("ALTER TABLE appointment_reminder_logs ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE appointment_reminder_logs FORCE ROW LEVEL SECURITY;");
            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation ON appointment_reminder_logs
                USING (tenant_id = current_setting('app.tenant_id', true)::uuid);");
            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE, DELETE ON appointment_reminder_logs TO furina_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON appointment_reminder_logs;");
            migrationBuilder.Sql("ALTER TABLE appointment_reminder_logs DISABLE ROW LEVEL SECURITY;");

            migrationBuilder.DropTable(
                name: "appointment_reminder_logs");
        }
    }
}
