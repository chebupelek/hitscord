using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class MessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_ChannelMessage_ChannelMessageId_TextChannelId",
                table: "Channel");

            migrationBuilder.DropForeignKey(
                name: "FK_ChannelVoteVariant_ChannelMessage_VoteId_TextChannelId",
                table: "ChannelVoteVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatVoteVariant_ChatMessage_VoteId_ChatId",
                table: "ChatVoteVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_File_ChannelMessage_ChannelMessageId_TextChannelId",
                table: "File");

            migrationBuilder.DropForeignKey(
                name: "FK_File_ChatMessage_ChatMessageId_ChatId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_File_ChannelMessageId_TextChannelId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_File_ChatMessageId_ChatId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_ChatVoteVariant_VoteId_ChatId",
                table: "ChatVoteVariant");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessage",
                table: "ChatMessage");

            migrationBuilder.DropIndex(
                name: "IX_ChannelVoteVariant_VoteId_TextChannelId",
                table: "ChannelVoteVariant");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChannelMessage",
                table: "ChannelMessage");

            migrationBuilder.DropIndex(
                name: "IX_Channel_ChannelMessageId_TextChannelId",
                table: "Channel");

            migrationBuilder.AddColumn<Guid>(
                name: "ChannelMessageRealId",
                table: "File",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChatMessageRealId",
                table: "File",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoteRealId",
                table: "ChatVoteVariant",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RealId",
                table: "ChatMessage",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "VoteRealId",
                table: "ChannelVoteVariant",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RealId",
                table: "ChannelMessage",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ChannelMessageRealId",
                table: "Channel",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessage",
                table: "ChatMessage",
                column: "RealId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChannelMessage",
                table: "ChannelMessage",
                column: "RealId");

            migrationBuilder.CreateIndex(
                name: "IX_File_ChannelMessageRealId",
                table: "File",
                column: "ChannelMessageRealId");

            migrationBuilder.CreateIndex(
                name: "IX_File_ChatMessageRealId",
                table: "File",
                column: "ChatMessageRealId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatVoteVariant_VoteRealId",
                table: "ChatVoteVariant",
                column: "VoteRealId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_Id_ChatId",
                table: "ChatMessage",
                columns: new[] { "Id", "ChatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelVoteVariant_VoteRealId",
                table: "ChannelVoteVariant",
                column: "VoteRealId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMessage_Id_TextChannelId",
                table: "ChannelMessage",
                columns: new[] { "Id", "TextChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channel_ChannelMessageRealId",
                table: "Channel",
                column: "ChannelMessageRealId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_ChannelMessage_ChannelMessageRealId",
                table: "Channel",
                column: "ChannelMessageRealId",
                principalTable: "ChannelMessage",
                principalColumn: "RealId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelVoteVariant_ChannelMessage_VoteRealId",
                table: "ChannelVoteVariant",
                column: "VoteRealId",
                principalTable: "ChannelMessage",
                principalColumn: "RealId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatVoteVariant_ChatMessage_VoteRealId",
                table: "ChatVoteVariant",
                column: "VoteRealId",
                principalTable: "ChatMessage",
                principalColumn: "RealId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_File_ChannelMessage_ChannelMessageRealId",
                table: "File",
                column: "ChannelMessageRealId",
                principalTable: "ChannelMessage",
                principalColumn: "RealId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_File_ChatMessage_ChatMessageRealId",
                table: "File",
                column: "ChatMessageRealId",
                principalTable: "ChatMessage",
                principalColumn: "RealId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_ChannelMessage_ChannelMessageRealId",
                table: "Channel");

            migrationBuilder.DropForeignKey(
                name: "FK_ChannelVoteVariant_ChannelMessage_VoteRealId",
                table: "ChannelVoteVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatVoteVariant_ChatMessage_VoteRealId",
                table: "ChatVoteVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_File_ChannelMessage_ChannelMessageRealId",
                table: "File");

            migrationBuilder.DropForeignKey(
                name: "FK_File_ChatMessage_ChatMessageRealId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_File_ChannelMessageRealId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_File_ChatMessageRealId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_ChatVoteVariant_VoteRealId",
                table: "ChatVoteVariant");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessage",
                table: "ChatMessage");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessage_Id_ChatId",
                table: "ChatMessage");

            migrationBuilder.DropIndex(
                name: "IX_ChannelVoteVariant_VoteRealId",
                table: "ChannelVoteVariant");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChannelMessage",
                table: "ChannelMessage");

            migrationBuilder.DropIndex(
                name: "IX_ChannelMessage_Id_TextChannelId",
                table: "ChannelMessage");

            migrationBuilder.DropIndex(
                name: "IX_Channel_ChannelMessageRealId",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "ChannelMessageRealId",
                table: "File");

            migrationBuilder.DropColumn(
                name: "ChatMessageRealId",
                table: "File");

            migrationBuilder.DropColumn(
                name: "VoteRealId",
                table: "ChatVoteVariant");

            migrationBuilder.DropColumn(
                name: "RealId",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "VoteRealId",
                table: "ChannelVoteVariant");

            migrationBuilder.DropColumn(
                name: "RealId",
                table: "ChannelMessage");

            migrationBuilder.DropColumn(
                name: "ChannelMessageRealId",
                table: "Channel");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessage",
                table: "ChatMessage",
                columns: new[] { "Id", "ChatId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChannelMessage",
                table: "ChannelMessage",
                columns: new[] { "Id", "TextChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_File_ChannelMessageId_TextChannelId",
                table: "File",
                columns: new[] { "ChannelMessageId", "TextChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_File_ChatMessageId_ChatId",
                table: "File",
                columns: new[] { "ChatMessageId", "ChatId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatVoteVariant_VoteId_ChatId",
                table: "ChatVoteVariant",
                columns: new[] { "VoteId", "ChatId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelVoteVariant_VoteId_TextChannelId",
                table: "ChannelVoteVariant",
                columns: new[] { "VoteId", "TextChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Channel_ChannelMessageId_TextChannelId",
                table: "Channel",
                columns: new[] { "ChannelMessageId", "TextChannelId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_ChannelMessage_ChannelMessageId_TextChannelId",
                table: "Channel",
                columns: new[] { "ChannelMessageId", "TextChannelId" },
                principalTable: "ChannelMessage",
                principalColumns: new[] { "Id", "TextChannelId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelVoteVariant_ChannelMessage_VoteId_TextChannelId",
                table: "ChannelVoteVariant",
                columns: new[] { "VoteId", "TextChannelId" },
                principalTable: "ChannelMessage",
                principalColumns: new[] { "Id", "TextChannelId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatVoteVariant_ChatMessage_VoteId_ChatId",
                table: "ChatVoteVariant",
                columns: new[] { "VoteId", "ChatId" },
                principalTable: "ChatMessage",
                principalColumns: new[] { "Id", "ChatId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_File_ChannelMessage_ChannelMessageId_TextChannelId",
                table: "File",
                columns: new[] { "ChannelMessageId", "TextChannelId" },
                principalTable: "ChannelMessage",
                principalColumns: new[] { "Id", "TextChannelId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_File_ChatMessage_ChatMessageId_ChatId",
                table: "File",
                columns: new[] { "ChatMessageId", "ChatId" },
                principalTable: "ChatMessage",
                principalColumns: new[] { "Id", "ChatId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
