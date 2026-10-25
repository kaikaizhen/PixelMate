using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixelMate.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DevicesCascadeOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_devices_users_owner_id",
                table: "devices");

            migrationBuilder.AddForeignKey(
                name: "fk_devices_users_owner_id",
                table: "devices",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_devices_users_owner_id",
                table: "devices");

            migrationBuilder.AddForeignKey(
                name: "fk_devices_users_owner_id",
                table: "devices",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
