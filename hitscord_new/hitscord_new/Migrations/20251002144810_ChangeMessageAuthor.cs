using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class ChangeMessageAuthor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelMessage_UserServer_AuthorId",
                table: "ChannelMessage");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelMessage_User_AuthorId",
                table: "ChannelMessage",
                column: "AuthorId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelMessage_User_AuthorId",
                table: "ChannelMessage");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelMessage_UserServer_AuthorId",
                table: "ChannelMessage",
                column: "AuthorId",
                principalTable: "UserServer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
