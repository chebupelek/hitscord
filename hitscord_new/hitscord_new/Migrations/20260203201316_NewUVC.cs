using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class NewUVC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MuteStatus",
                table: "UserVoiceChannel");

            migrationBuilder.AddColumn<bool>(
                name: "Inside",
                table: "UserVoiceChannel",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MutedHimself",
                table: "UserVoiceChannel",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MutedOther",
                table: "UserVoiceChannel",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Inside",
                table: "UserVoiceChannel");

            migrationBuilder.DropColumn(
                name: "MutedHimself",
                table: "UserVoiceChannel");

            migrationBuilder.DropColumn(
                name: "MutedOther",
                table: "UserVoiceChannel");

            migrationBuilder.AddColumn<int>(
                name: "MuteStatus",
                table: "UserVoiceChannel",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
