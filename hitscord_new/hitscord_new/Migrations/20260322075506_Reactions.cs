using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class Reactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelMessageReaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChannelMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReactionCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMessageReaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelMessageReaction_ChannelMessage_ChannelMessageId",
                        column: x => x.ChannelMessageId,
                        principalTable: "ChannelMessage",
                        principalColumn: "RealId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelMessageReaction_User_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessageReaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChatMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReactionCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessageReaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessageReaction_ChatMessage_ChatMessageId",
                        column: x => x.ChatMessageId,
                        principalTable: "ChatMessage",
                        principalColumn: "RealId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessageReaction_User_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMessageReaction_AuthorId",
                table: "ChannelMessageReaction",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMessageReaction_ChannelMessageId",
                table: "ChannelMessageReaction",
                column: "ChannelMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReaction_AuthorId",
                table: "ChatMessageReaction",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReaction_ChatMessageId",
                table: "ChatMessageReaction",
                column: "ChatMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelMessageReaction");

            migrationBuilder.DropTable(
                name: "ChatMessageReaction");
        }
    }
}
