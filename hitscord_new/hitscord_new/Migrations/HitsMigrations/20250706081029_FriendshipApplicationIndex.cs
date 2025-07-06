using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class FriendshipApplicationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Friendship_UserIdFrom",
                table: "Friendship");

            migrationBuilder.CreateIndex(
                name: "IX_Friendship_UserIdFrom_UserIdTo",
                table: "Friendship",
                columns: new[] { "UserIdFrom", "UserIdTo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Friendship_UserIdFrom_UserIdTo",
                table: "Friendship");

            migrationBuilder.CreateIndex(
                name: "IX_Friendship_UserIdFrom",
                table: "Friendship",
                column: "UserIdFrom");
        }
    }
}
