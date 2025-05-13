using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class Notification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RoleTag",
                table: "Role",
                newName: "Tag");

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "UserServer",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "UserServer");

            migrationBuilder.RenameColumn(
                name: "Tag",
                table: "Role",
                newName: "RoleTag");
        }
    }
}
