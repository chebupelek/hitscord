using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class Friendship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FriendshipApplication",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NonFriendMessage",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Notifiable",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Friendship",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserIdFrom = table.Column<Guid>(type: "uuid", nullable: false),
                    UserIdTo = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendship", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Friendship_User_UserIdFrom",
                        column: x => x.UserIdFrom,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Friendship_User_UserIdTo",
                        column: x => x.UserIdTo,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Friendship_UserIdFrom",
                table: "Friendship",
                column: "UserIdFrom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendship_UserIdTo",
                table: "Friendship",
                column: "UserIdTo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Friendship");

            migrationBuilder.DropColumn(
                name: "FriendshipApplication",
                table: "User");

            migrationBuilder.DropColumn(
                name: "NonFriendMessage",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Notifiable",
                table: "User");
        }
    }
}
