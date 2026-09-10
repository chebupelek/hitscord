using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class taskid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TaskMessageRealId",
                table: "File",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_File_TaskMessageRealId",
                table: "File",
                column: "TaskMessageRealId");

            migrationBuilder.AddForeignKey(
                name: "FK_File_LessonChannelMessage_TaskMessageRealId",
                table: "File",
                column: "TaskMessageRealId",
                principalTable: "LessonChannelMessage",
                principalColumn: "RealId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_File_LessonChannelMessage_TaskMessageRealId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_File_TaskMessageRealId",
                table: "File");

            migrationBuilder.DropColumn(
                name: "TaskMessageRealId",
                table: "File");
        }
    }
}
