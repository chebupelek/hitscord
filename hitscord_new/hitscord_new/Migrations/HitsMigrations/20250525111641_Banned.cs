using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class Banned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                table: "UserServer",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BanTime",
                table: "UserServer",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanReason",
                table: "UserServer");

            migrationBuilder.DropColumn(
                name: "BanTime",
                table: "UserServer");
        }
    }
}
