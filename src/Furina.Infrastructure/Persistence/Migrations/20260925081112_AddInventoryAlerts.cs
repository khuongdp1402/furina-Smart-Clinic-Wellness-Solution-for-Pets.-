using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Furina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "low_stock_alert_lead_days",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<int>(
                name: "min_stock_threshold",
                table: "inventory_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "inventory_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    inventory_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_alerts_inventory_batches_inventory_batch_id",
                        column: x => x.inventory_batch_id,
                        principalTable: "inventory_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_alerts_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_alerts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_alerts_inventory_batch_id",
                table: "inventory_alerts",
                column: "inventory_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_alerts_inventory_item_id",
                table: "inventory_alerts",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_alerts_tenant_id",
                table: "inventory_alerts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_alerts_type_inventory_batch_id_resolved_at",
                table: "inventory_alerts",
                columns: new[] { "type", "inventory_batch_id", "resolved_at" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_alerts_type_inventory_item_id_resolved_at",
                table: "inventory_alerts",
                columns: new[] { "type", "inventory_item_id", "resolved_at" });

            migrationBuilder.Sql("ALTER TABLE inventory_alerts ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE inventory_alerts FORCE ROW LEVEL SECURITY;");
            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation ON inventory_alerts
                USING (tenant_id = current_setting('app.tenant_id', true)::uuid);");
            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE, DELETE ON inventory_alerts TO furina_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation ON inventory_alerts;");
            migrationBuilder.Sql("ALTER TABLE inventory_alerts DISABLE ROW LEVEL SECURITY;");

            migrationBuilder.DropTable(
                name: "inventory_alerts");

            migrationBuilder.DropColumn(
                name: "low_stock_alert_lead_days",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "min_stock_threshold",
                table: "inventory_items");
        }
    }
}
