using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class AdminOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationsHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Mail = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    OperationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationsHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationsHistory_Admin_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Admin",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationsHistory_AdminId",
                table: "OperationsHistory",
                column: "AdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationsHistory");
        }
    }
}
