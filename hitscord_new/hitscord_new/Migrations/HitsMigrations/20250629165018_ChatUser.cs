using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class ChatUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Chat_ChatDbModelId",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_ChatDbModelId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ChatDbModelId",
                table: "User");

            migrationBuilder.CreateTable(
                name: "UserChat",
                columns: table => new
                {
                    ChatDbModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChat", x => new { x.ChatDbModelId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserChat_Chat_ChatDbModelId",
                        column: x => x.ChatDbModelId,
                        principalTable: "Chat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserChat_User_UsersId",
                        column: x => x.UsersId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserChat_UsersId",
                table: "UserChat",
                column: "UsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserChat");

            migrationBuilder.AddColumn<Guid>(
                name: "ChatDbModelId",
                table: "User",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_ChatDbModelId",
                table: "User",
                column: "ChatDbModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Chat_ChatDbModelId",
                table: "User",
                column: "ChatDbModelId",
                principalTable: "Chat",
                principalColumn: "Id");
        }
    }
}
