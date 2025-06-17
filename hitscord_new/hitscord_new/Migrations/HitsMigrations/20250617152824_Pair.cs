using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class Pair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatDbModelUserDbModel");

            migrationBuilder.AddColumn<Guid>(
                name: "ChatDbModelId",
                table: "User",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pair",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PairVoiceChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    FilterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<string>(type: "text", nullable: false),
                    Starts = table.Column<long>(type: "bigint", nullable: false),
                    Ends = table.Column<long>(type: "bigint", nullable: false),
                    LessonNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pair", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pair_Channel_PairVoiceChannelId",
                        column: x => x.PairVoiceChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pair_Server_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Server",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PairDbModelRoleDbModel",
                columns: table => new
                {
                    PairDbModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PairDbModelRoleDbModel", x => new { x.PairDbModelId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_PairDbModelRoleDbModel_Pair_PairDbModelId",
                        column: x => x.PairDbModelId,
                        principalTable: "Pair",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PairDbModelRoleDbModel_Role_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PairUser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PairId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeEnter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PairUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PairUser_Pair_PairId",
                        column: x => x.PairId,
                        principalTable: "Pair",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PairUser_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_ChatDbModelId",
                table: "User",
                column: "ChatDbModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Pair_PairVoiceChannelId",
                table: "Pair",
                column: "PairVoiceChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Pair_ServerId",
                table: "Pair",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_PairDbModelRoleDbModel_RolesId",
                table: "PairDbModelRoleDbModel",
                column: "RolesId");

            migrationBuilder.CreateIndex(
                name: "IX_PairUser_PairId",
                table: "PairUser",
                column: "PairId");

            migrationBuilder.CreateIndex(
                name: "IX_PairUser_UserId",
                table: "PairUser",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Chat_ChatDbModelId",
                table: "User",
                column: "ChatDbModelId",
                principalTable: "Chat",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Chat_ChatDbModelId",
                table: "User");

            migrationBuilder.DropTable(
                name: "PairDbModelRoleDbModel");

            migrationBuilder.DropTable(
                name: "PairUser");

            migrationBuilder.DropTable(
                name: "Pair");

            migrationBuilder.DropIndex(
                name: "IX_User_ChatDbModelId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ChatDbModelId",
                table: "User");

            migrationBuilder.CreateTable(
                name: "ChatDbModelUserDbModel",
                columns: table => new
                {
                    ChatDbModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatDbModelUserDbModel", x => new { x.ChatDbModelId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_ChatDbModelUserDbModel_Chat_ChatDbModelId",
                        column: x => x.ChatDbModelId,
                        principalTable: "Chat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatDbModelUserDbModel_User_UsersId",
                        column: x => x.UsersId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatDbModelUserDbModel_UsersId",
                table: "ChatDbModelUserDbModel",
                column: "UsersId");
        }
    }
}
