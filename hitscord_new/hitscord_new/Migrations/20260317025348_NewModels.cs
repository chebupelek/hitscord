using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class NewModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelMessage_Channel_TextChannelId",
                table: "ChannelMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_Chat_ChatId",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "TextChannelIdDouble",
                table: "ChannelMessage");

            migrationBuilder.AlterColumn<Guid>(
                name: "TextChannelId",
                table: "ChannelMessage",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelMessage_Channel_TextChannelId",
                table: "ChannelMessage",
                column: "TextChannelId",
                principalTable: "Channel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_Chat_ChatId",
                table: "ChatMessage",
                column: "ChatId",
                principalTable: "Chat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelMessage_Channel_TextChannelId",
                table: "ChannelMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_Chat_ChatId",
                table: "ChatMessage");

            migrationBuilder.DropTable(
                name: "ChannelMessageReaction");

            migrationBuilder.DropTable(
                name: "ChatMessageReaction");

            migrationBuilder.AlterColumn<Guid>(
                name: "TextChannelId",
                table: "ChannelMessage",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "TextChannelIdDouble",
                table: "ChannelMessage",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelMessage_Channel_TextChannelId",
                table: "ChannelMessage",
                column: "TextChannelId",
                principalTable: "Channel",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_Chat_ChatId",
                table: "ChatMessage",
                column: "ChatId",
                principalTable: "Chat",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
