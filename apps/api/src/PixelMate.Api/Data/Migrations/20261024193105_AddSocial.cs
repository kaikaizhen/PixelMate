using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixelMate.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "letters",
                columns: table => new
                {
                    letter_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    sender_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    recipient_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    body = table.Column<string>(type: "varchar(2400)", maxLength: 2400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    paper_item_id = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gift_item_id = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    deliver_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    read_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sender_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    recipient_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_letters", x => x.letter_id);
                    table.ForeignKey(
                        name: "fk_letters_item_definitions_gift_item_id",
                        column: x => x.gift_item_id,
                        principalTable: "item_definitions",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_letters_item_definitions_paper_item_id",
                        column: x => x.paper_item_id,
                        principalTable: "item_definitions",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_letters_users_recipient_id",
                        column: x => x.recipient_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_letters_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pairing_invites",
                columns: table => new
                {
                    invite_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    inviter_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kind = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    used_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    used_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pairing_invites", x => x.invite_id);
                    table.ForeignKey(
                        name: "fk_pairing_invites_users_inviter_id",
                        column: x => x.inviter_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "relationships",
                columns: table => new
                {
                    relationship_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    user_low_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    user_high_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    kind = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ended_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_relationships", x => x.relationship_id);
                    table.CheckConstraint("ck_relationships_order", "user_low_id < user_high_id");
                    table.ForeignKey(
                        name: "fk_relationships_users_user_high_id",
                        column: x => x.user_high_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_relationships_users_user_low_id",
                        column: x => x.user_low_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_letters_gift_item_id",
                table: "letters",
                column: "gift_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_letters_paper_item_id",
                table: "letters",
                column: "paper_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_letters_recipient_id_deliver_at",
                table: "letters",
                columns: new[] { "recipient_id", "deliver_at" });

            migrationBuilder.CreateIndex(
                name: "ix_letters_sender_id_idempotency_key",
                table: "letters",
                columns: new[] { "sender_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_letters_sender_id_sent_at",
                table: "letters",
                columns: new[] { "sender_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_pairing_invites_code_hash",
                table: "pairing_invites",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pairing_invites_inviter_id",
                table: "pairing_invites",
                column: "inviter_id");

            migrationBuilder.CreateIndex(
                name: "ix_relationships_user_high_id",
                table: "relationships",
                column: "user_high_id");

            migrationBuilder.CreateIndex(
                name: "ix_relationships_user_low_id_user_high_id",
                table: "relationships",
                columns: new[] { "user_low_id", "user_high_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "letters");

            migrationBuilder.DropTable(
                name: "pairing_invites");

            migrationBuilder.DropTable(
                name: "relationships");
        }
    }
}
