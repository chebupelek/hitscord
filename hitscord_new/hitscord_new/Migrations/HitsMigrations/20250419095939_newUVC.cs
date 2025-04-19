using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class newUVC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserVoiceChannel",
                table: "UserVoiceChannel");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserVoiceChannel",
                table: "UserVoiceChannel",
                columns: new[] { "UserId", "VoiceChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserVoiceChannel_UserId",
                table: "UserVoiceChannel",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserVoiceChannel",
                table: "UserVoiceChannel");

            migrationBuilder.DropIndex(
                name: "IX_UserVoiceChannel_UserId",
                table: "UserVoiceChannel");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserVoiceChannel",
                table: "UserVoiceChannel",
                column: "UserId");
        }
    }
}
