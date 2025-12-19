using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class AdminOperationNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mail",
                table: "OperationsHistory");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "OperationsHistory",
                newName: "OperationData");

            migrationBuilder.AddColumn<string>(
                name: "Operation",
                table: "OperationsHistory",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Operation",
                table: "OperationsHistory");

            migrationBuilder.RenameColumn(
                name: "OperationData",
                table: "OperationsHistory",
                newName: "PasswordHash");

            migrationBuilder.AddColumn<string>(
                name: "Mail",
                table: "OperationsHistory",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
