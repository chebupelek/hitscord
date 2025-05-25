using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Message.Migrations
{
    /// <inheritdoc />
    public partial class MessageSecond : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Roles",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "UserIds",
                table: "Messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid[]>(
                name: "Roles",
                table: "Messages",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<Guid[]>(
                name: "UserIds",
                table: "Messages",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);
        }
    }
}
