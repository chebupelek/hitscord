﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_net.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Token",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Token", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnnouncementChannelDbModelRoleDbModel",
                columns: table => new
                {
                    AnnouncementChannelDbModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolesToNotifyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementChannelDbModelRoleDbModel", x => new { x.AnnouncementChannelDbModelId, x.RolesToNotifyId });
                    table.ForeignKey(
                        name: "FK_AnnouncementChannelDbModelRoleDbModel_Role_RolesToNotifyId",
                        column: x => x.RolesToNotifyId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Channel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelType = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    IsMessage = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelDbModelRoleDbModel",
                columns: table => new
                {
                    ChannelDbModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolesCanViewId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelDbModelRoleDbModel", x => new { x.ChannelDbModelId, x.RolesCanViewId });
                    table.ForeignKey(
                        name: "FK_ChannelDbModelRoleDbModel_Channel_ChannelDbModelId",
                        column: x => x.ChannelDbModelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelDbModelRoleDbModel_Role_RolesCanViewId",
                        column: x => x.RolesCanViewId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelDbModelRoleDbModel1",
                columns: table => new
                {
                    ChannelDbModel1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RolesCanWriteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelDbModelRoleDbModel1", x => new { x.ChannelDbModel1Id, x.RolesCanWriteId });
                    table.ForeignKey(
                        name: "FK_ChannelDbModelRoleDbModel1_Channel_ChannelDbModel1Id",
                        column: x => x.ChannelDbModel1Id,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelDbModelRoleDbModel1_Role_RolesCanWriteId",
                        column: x => x.RolesCanWriteId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Mail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    AccountName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccountTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccountCreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VoiceChannelDbModelId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Channel_VoiceChannelDbModelId",
                        column: x => x.VoiceChannelDbModelId,
                        principalTable: "Channel",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Server",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Server", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Server_User_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserServer",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserServerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserServer", x => new { x.UserId, x.ServerId });
                    table.ForeignKey(
                        name: "FK_UserServer_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserServer_Server_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Server",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserServer_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementChannelDbModelRoleDbModel_RolesToNotifyId",
                table: "AnnouncementChannelDbModelRoleDbModel",
                column: "RolesToNotifyId");

            migrationBuilder.CreateIndex(
                name: "IX_Channel_ServerId",
                table: "Channel",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDbModelRoleDbModel_RolesCanViewId",
                table: "ChannelDbModelRoleDbModel",
                column: "RolesCanViewId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDbModelRoleDbModel1_RolesCanWriteId",
                table: "ChannelDbModelRoleDbModel1",
                column: "RolesCanWriteId");

            migrationBuilder.CreateIndex(
                name: "IX_Server_CreatorId",
                table: "Server",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_User_VoiceChannelDbModelId",
                table: "User",
                column: "VoiceChannelDbModelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserServer_RoleId",
                table: "UserServer",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserServer_ServerId",
                table: "UserServer",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnnouncementChannelDbModelRoleDbModel_Channel_AnnouncementC~",
                table: "AnnouncementChannelDbModelRoleDbModel",
                column: "AnnouncementChannelDbModelId",
                principalTable: "Channel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_Server_ServerId",
                table: "Channel",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_Channel_VoiceChannelDbModelId",
                table: "User");

            migrationBuilder.DropTable(
                name: "AnnouncementChannelDbModelRoleDbModel");

            migrationBuilder.DropTable(
                name: "ChannelDbModelRoleDbModel");

            migrationBuilder.DropTable(
                name: "ChannelDbModelRoleDbModel1");

            migrationBuilder.DropTable(
                name: "Token");

            migrationBuilder.DropTable(
                name: "UserServer");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Channel");

            migrationBuilder.DropTable(
                name: "Server");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}