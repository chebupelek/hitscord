using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class RolesUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Role",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoleTag",
                table: "Role",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "RoleTag",
                table: "Role");
        }
    }
}
