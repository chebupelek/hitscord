using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class FileTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_File_LessonChannelMessage_LessonChannelMessageSolutionDbMod~",
                table: "File");

            migrationBuilder.DropForeignKey(
                name: "FK_File_LessonChannelMessage_LessonChannelMessageTaskDbModelRe~",
                table: "File");

            migrationBuilder.DropForeignKey(
                name: "FK_File_LessonChannelMessage_TaskMessageRealId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_File_LessonChannelMessageSolutionDbModelRealId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_File_LessonChannelMessageTaskDbModelRealId",
                table: "File");

            migrationBuilder.DropColumn(
                name: "LessonChannelMessageSolutionDbModelRealId",
                table: "File");

            migrationBuilder.DropColumn(
                name: "LessonChannelMessageTaskDbModelRealId",
                table: "File");

            migrationBuilder.AddForeignKey(
                name: "FK_File_LessonChannelMessage_TaskMessageRealId",
                table: "File",
                column: "TaskMessageRealId",
                principalTable: "LessonChannelMessage",
                principalColumn: "RealId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_File_LessonChannelMessage_TaskMessageRealId",
                table: "File");

            migrationBuilder.AddColumn<Guid>(
                name: "LessonChannelMessageSolutionDbModelRealId",
                table: "File",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LessonChannelMessageTaskDbModelRealId",
                table: "File",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_File_LessonChannelMessageSolutionDbModelRealId",
                table: "File",
                column: "LessonChannelMessageSolutionDbModelRealId");

            migrationBuilder.CreateIndex(
                name: "IX_File_LessonChannelMessageTaskDbModelRealId",
                table: "File",
                column: "LessonChannelMessageTaskDbModelRealId");

            migrationBuilder.AddForeignKey(
                name: "FK_File_LessonChannelMessage_LessonChannelMessageSolutionDbMod~",
                table: "File",
                column: "LessonChannelMessageSolutionDbModelRealId",
                principalTable: "LessonChannelMessage",
                principalColumn: "RealId");

            migrationBuilder.AddForeignKey(
                name: "FK_File_LessonChannelMessage_LessonChannelMessageTaskDbModelRe~",
                table: "File",
                column: "LessonChannelMessageTaskDbModelRealId",
                principalTable: "LessonChannelMessage",
                principalColumn: "RealId");

            migrationBuilder.AddForeignKey(
                name: "FK_File_LessonChannelMessage_TaskMessageRealId",
                table: "File",
                column: "TaskMessageRealId",
                principalTable: "LessonChannelMessage",
                principalColumn: "RealId");
        }
    }
}
