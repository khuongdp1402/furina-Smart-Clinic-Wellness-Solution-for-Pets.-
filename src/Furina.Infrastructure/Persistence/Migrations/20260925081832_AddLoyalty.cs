using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Furina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "loyalty_points_amount_unit",
                table: "tenants",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 10000m);

            migrationBuilder.AddColumn<int>(
                name: "loyalty_points_per_unit",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "invoices",
                type: "text",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "loyalty_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points_balance = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loyalty_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_loyalty_accounts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loyalty_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loyalty_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_loyalty_transactions_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_loyalty_transactions_loyalty_accounts_loyalty_account_id",
                        column: x => x.loyalty_account_id,
                        principalTable: "loyalty_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_loyalty_transactions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_accounts_owner_id",
                table: "loyalty_accounts",
                column: "owner_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_accounts_tenant_id",
                table: "loyalty_accounts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_transactions_invoice_id",
                table: "loyalty_transactions",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_transactions_loyalty_account_id",
                table: "loyalty_transactions",
                column: "loyalty_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_transactions_tenant_id",
                table: "loyalty_transactions",
                column: "tenant_id");

            foreach (var table in new[] { "loyalty_accounts", "loyalty_transactions" })
            {
                migrationBuilder.Sql($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE {table} FORCE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($@"
                    CREATE POLICY tenant_isolation ON {table}
                    USING (tenant_id = current_setting('app.tenant_id', true)::uuid);");
            }
            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE, DELETE ON loyalty_accounts, loyalty_transactions TO furina_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "loyalty_accounts", "loyalty_transactions" })
            {
                migrationBuilder.Sql($"DROP POLICY IF EXISTS tenant_isolation ON {table};");
                migrationBuilder.Sql($"ALTER TABLE {table} DISABLE ROW LEVEL SECURITY;");
            }

            migrationBuilder.DropTable(
                name: "loyalty_transactions");

            migrationBuilder.DropTable(
                name: "loyalty_accounts");

            migrationBuilder.DropColumn(
                name: "loyalty_points_amount_unit",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "loyalty_points_per_unit",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "status",
                table: "invoices");
        }
    }
}
