using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixelMate.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pets",
                columns: table => new
                {
                    pet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    owner_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_variant = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<int>(type: "int", nullable: false),
                    xp = table.Column<int>(type: "int", nullable: false),
                    revision = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pets", x => x.pet_id);
                    table.ForeignKey(
                        name: "fk_pets_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pet_skills",
                columns: table => new
                {
                    pet_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    skill_key = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    points = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pet_skills", x => new { x.pet_id, x.skill_key });
                    table.CheckConstraint("ck_pet_skills_points", "points >= 0 AND points <= 5");
                    table.ForeignKey(
                        name: "fk_pet_skills_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "pet_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_pets_owner_id",
                table: "pets",
                column: "owner_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pet_skills");

            migrationBuilder.DropTable(
                name: "pets");
        }
    }
}
