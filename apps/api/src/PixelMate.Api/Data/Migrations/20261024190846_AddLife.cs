using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixelMate.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLife : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pet_events",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    pet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    slot = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    item_id = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pet_events", x => x.event_id);
                    table.ForeignKey(
                        name: "fk_pet_events_item_definitions_item_id",
                        column: x => x.item_id,
                        principalTable: "item_definitions",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pet_events_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "pet_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pet_life",
                columns: table => new
                {
                    pet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    cursor_slot = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pet_life", x => x.pet_id);
                    table.ForeignKey(
                        name: "fk_pet_life_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "pet_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_pet_events_item_id",
                table: "pet_events",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_pet_events_pet_id_occurred_at",
                table: "pet_events",
                columns: new[] { "pet_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_pet_events_pet_id_slot",
                table: "pet_events",
                columns: new[] { "pet_id", "slot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pet_events");

            migrationBuilder.DropTable(
                name: "pet_life");
        }
    }
}
