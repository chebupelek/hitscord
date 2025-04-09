using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class NewUserVoiceChannelSystem2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserVoiceChannel_Channel_VoiceChannelDbModelId",
                table: "UserVoiceChannel");

            migrationBuilder.DropIndex(
                name: "IX_UserVoiceChannel_VoiceChannelDbModelId",
                table: "UserVoiceChannel");

            migrationBuilder.DropColumn(
                name: "VoiceChannelDbModelId",
                table: "UserVoiceChannel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VoiceChannelDbModelId",
                table: "UserVoiceChannel",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserVoiceChannel_VoiceChannelDbModelId",
                table: "UserVoiceChannel",
                column: "VoiceChannelDbModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserVoiceChannel_Channel_VoiceChannelDbModelId",
                table: "UserVoiceChannel",
                column: "VoiceChannelDbModelId",
                principalTable: "Channel",
                principalColumn: "Id");
        }
    }
}
