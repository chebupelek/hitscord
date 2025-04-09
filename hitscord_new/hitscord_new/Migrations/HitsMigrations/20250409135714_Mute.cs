using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class Mute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Channel_VoiceChannelDbModelId",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_VoiceChannelDbModelId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "VoiceChannelDbModelId",
                table: "User");

            migrationBuilder.CreateTable(
                name: "UserVoiceChannel",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoiceChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    MuteStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVoiceChannel", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserVoiceChannel_Channel_VoiceChannelId",
                        column: x => x.VoiceChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserVoiceChannel_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserVoiceChannel_VoiceChannelId",
                table: "UserVoiceChannel",
                column: "VoiceChannelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserVoiceChannel");

            migrationBuilder.AddColumn<Guid>(
                name: "VoiceChannelDbModelId",
                table: "User",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_VoiceChannelDbModelId",
                table: "User",
                column: "VoiceChannelDbModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Channel_VoiceChannelDbModelId",
                table: "User",
                column: "VoiceChannelDbModelId",
                principalTable: "Channel",
                principalColumn: "Id");
        }
    }
}
