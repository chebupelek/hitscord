using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class IdDouble : Migration
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

            migrationBuilder.AddColumn<Guid>(
                name: "ChatIdDouble",
                table: "ChatMessage",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelMessage_Channel_TextChannelId",
                table: "ChannelMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_Chat_ChatId",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "ChatIdDouble",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "TextChannelIdDouble",
                table: "ChannelMessage");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelMessage_Channel_TextChannelId",
                table: "ChannelMessage",
                column: "TextChannelId",
                principalTable: "Channel",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_Chat_ChatId",
                table: "ChatMessage",
                column: "ChatId",
                principalTable: "Chat",
                principalColumn: "Id");
        }
    }
}
