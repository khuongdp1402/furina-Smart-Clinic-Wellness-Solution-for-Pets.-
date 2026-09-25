using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Furina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "scheduled_at",
                table: "appointments",
                newName: "start_time");

            migrationBuilder.RenameIndex(
                name: "IX_appointments_clinic_id_scheduled_at",
                table: "appointments",
                newName: "IX_appointments_clinic_id_start_time");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "end_time",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "pet_id",
                table: "appointments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "vet_user_id",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointments_pet_id",
                table: "appointments",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_vet_user_id_start_time",
                table: "appointments",
                columns: new[] { "vet_user_id", "start_time" });

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_pets_pet_id",
                table: "appointments",
                column: "pet_id",
                principalTable: "pets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_users_vet_user_id",
                table: "appointments",
                column: "vet_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_pets_pet_id",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_users_vet_user_id",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_pet_id",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_vet_user_id_start_time",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "end_time",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "pet_id",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "vet_user_id",
                table: "appointments");

            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "appointments",
                newName: "scheduled_at");

            migrationBuilder.RenameIndex(
                name: "IX_appointments_clinic_id_start_time",
                table: "appointments",
                newName: "IX_appointments_clinic_id_scheduled_at");
        }
    }
}
