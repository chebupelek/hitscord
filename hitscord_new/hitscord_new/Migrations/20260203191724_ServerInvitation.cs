using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class ServerInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelMessage_User_AuthorId",
                table: "ChannelMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_User_AuthorId",
                table: "ChatMessage");

            migrationBuilder.AddColumn<Guid>(
                name: "InvitationId",
                table: "UserServer",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InvitationId",
                table: "ServerApplications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ServerCanUseInvitations",
                table: "Role",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorId",
                table: "ChatMessage",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorId",
                table: "ChannelMessage",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "Invitation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitation_Server_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Server",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invitation_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserServer_InvitationId",
                table: "UserServer",
                column: "InvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerApplications_InvitationId",
                table: "ServerApplications",
                column: "InvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitation_ServerId",
                table: "Invitation",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitation_UserId",
                table: "Invitation",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelMessage_User_AuthorId",
                table: "ChannelMessage",
                column: "AuthorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_User_AuthorId",
                table: "ChatMessage",
                column: "AuthorId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServerApplications_Invitation_InvitationId",
                table: "ServerApplications",
                column: "InvitationId",
                principalTable: "Invitation",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserServer_Invitation_InvitationId",
                table: "UserServer",
                column: "InvitationId",
                principalTable: "Invitation",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelMessage_User_AuthorId",
                table: "ChannelMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_User_AuthorId",
                table: "ChatMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerApplications_Invitation_InvitationId",
                table: "ServerApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_UserServer_Invitation_InvitationId",
                table: "UserServer");

            migrationBuilder.DropTable(
                name: "Invitation");

            migrationBuilder.DropIndex(
                name: "IX_UserServer_InvitationId",
                table: "UserServer");

            migrationBuilder.DropIndex(
                name: "IX_ServerApplications_InvitationId",
                table: "ServerApplications");

            migrationBuilder.DropColumn(
                name: "InvitationId",
                table: "UserServer");

            migrationBuilder.DropColumn(
                name: "InvitationId",
                table: "ServerApplications");

            migrationBuilder.DropColumn(
                name: "ServerCanUseInvitations",
                table: "Role");

            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorId",
                table: "ChatMessage",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorId",
                table: "ChannelMessage",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelMessage_User_AuthorId",
                table: "ChannelMessage",
                column: "AuthorId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_User_AuthorId",
                table: "ChatMessage",
                column: "AuthorId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
