using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class Details : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "JoinTime",
                table: "UserServer",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "LessonChannelMessageTaskDbModelRealId",
                table: "Role",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Role",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ServerCanCheckGrades",
                table: "Role",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LessonChannelMessageSolutionDbModelRealId",
                table: "File",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LessonChannelMessageTaskDbModelRealId",
                table: "File",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Channel",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Channel",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TextLessonChannelDbModel_DeleteTime",
                table: "Channel",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChannelCanJoinQueue",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextQueueChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelCanJoinQueue", x => new { x.RoleId, x.TextQueueChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelCanJoinQueue_Channel_TextQueueChannelId",
                        column: x => x.TextQueueChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCanJoinQueue_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelCanMakeTasks",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextLessonChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelCanMakeTasks", x => new { x.RoleId, x.TextLessonChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelCanMakeTasks_Channel_TextLessonChannelId",
                        column: x => x.TextLessonChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCanMakeTasks_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelCanTakeFromQueue",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextQueueChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelCanTakeFromQueue", x => new { x.RoleId, x.TextQueueChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelCanTakeFromQueue_Channel_TextQueueChannelId",
                        column: x => x.TextQueueChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCanTakeFromQueue_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelGroup_Server_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Server",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonChannelMessage",
                columns: table => new
                {
                    RealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    TextLessonChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplyToMessageId = table.Column<long>(type: "bigint", nullable: true),
                    DeleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MessageType = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Grade = table.Column<int>(type: "integer", nullable: true),
                    GradeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GradeAuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LessonChannelMessageTaskDbModel_Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    LessonChannelMessageTaskDbModel_UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonChannelMessage", x => x.RealId);
                    table.ForeignKey(
                        name: "FK_LessonChannelMessage_Channel_TextLessonChannelId",
                        column: x => x.TextLessonChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonChannelMessage_User_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LessonChannelMessage_User_GradeAuthorId",
                        column: x => x.GradeAuthorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QueueItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueItem_Channel_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueueItem_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueueTake",
                columns: table => new
                {
                    TextQueueChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    TakerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromQueueId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueTake", x => new { x.TextQueueChannelId, x.TakerId, x.FromQueueId });
                    table.ForeignKey(
                        name: "FK_QueueTake_Channel_TextQueueChannelId",
                        column: x => x.TextQueueChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueueTake_User_FromQueueId",
                        column: x => x.FromQueueId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QueueTake_User_TakerId",
                        column: x => x.TakerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDeviceToken_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Role_LessonChannelMessageTaskDbModelRealId",
                table: "Role",
                column: "LessonChannelMessageTaskDbModelRealId");

            migrationBuilder.CreateIndex(
                name: "IX_File_LessonChannelMessageSolutionDbModelRealId",
                table: "File",
                column: "LessonChannelMessageSolutionDbModelRealId");

            migrationBuilder.CreateIndex(
                name: "IX_File_LessonChannelMessageTaskDbModelRealId",
                table: "File",
                column: "LessonChannelMessageTaskDbModelRealId");

            migrationBuilder.CreateIndex(
                name: "IX_Channel_GroupId",
                table: "Channel",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCanJoinQueue_TextQueueChannelId",
                table: "ChannelCanJoinQueue",
                column: "TextQueueChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCanMakeTasks_TextLessonChannelId",
                table: "ChannelCanMakeTasks",
                column: "TextLessonChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCanTakeFromQueue_TextQueueChannelId",
                table: "ChannelCanTakeFromQueue",
                column: "TextQueueChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelGroup_ServerId",
                table: "ChannelGroup",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonChannelMessage_AuthorId",
                table: "LessonChannelMessage",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonChannelMessage_GradeAuthorId",
                table: "LessonChannelMessage",
                column: "GradeAuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonChannelMessage_Id_TextLessonChannelId",
                table: "LessonChannelMessage",
                columns: new[] { "Id", "TextLessonChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonChannelMessage_TextLessonChannelId",
                table: "LessonChannelMessage",
                column: "TextLessonChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueItem_ChannelId",
                table: "QueueItem",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueItem_UserId",
                table: "QueueItem",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueTake_FromQueueId",
                table: "QueueTake",
                column: "FromQueueId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueTake_TakerId",
                table: "QueueTake",
                column: "TakerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceToken_UserId",
                table: "UserDeviceToken",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_ChannelGroup_GroupId",
                table: "Channel",
                column: "GroupId",
                principalTable: "ChannelGroup",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_File_LessonChannelMessage_LessonChannelMessageSolutionDbMod~",
                table: "File",
                column: "LessonChannelMessageSolutionDbModelRealId",
                principalTable: "LessonChannelMessage",
                principalColumn: "RealId");

            migrationBuilder.AddForeignKey(
                name: "FK_File_LessonChannelMessage_LessonChannelMessageTaskDbModelRe~",
                table: "File",
                column: "LessonChannelMessageTaskDbModelRealId",
                principalTable: "LessonChannelMessage",
                principalColumn: "RealId");

            migrationBuilder.AddForeignKey(
                name: "FK_Role_LessonChannelMessage_LessonChannelMessageTaskDbModelRe~",
                table: "Role",
                column: "LessonChannelMessageTaskDbModelRealId",
                principalTable: "LessonChannelMessage",
                principalColumn: "RealId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_ChannelGroup_GroupId",
                table: "Channel");

            migrationBuilder.DropForeignKey(
                name: "FK_File_LessonChannelMessage_LessonChannelMessageSolutionDbMod~",
                table: "File");

            migrationBuilder.DropForeignKey(
                name: "FK_File_LessonChannelMessage_LessonChannelMessageTaskDbModelRe~",
                table: "File");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_LessonChannelMessage_LessonChannelMessageTaskDbModelRe~",
                table: "Role");

            migrationBuilder.DropTable(
                name: "ChannelCanJoinQueue");

            migrationBuilder.DropTable(
                name: "ChannelCanMakeTasks");

            migrationBuilder.DropTable(
                name: "ChannelCanTakeFromQueue");

            migrationBuilder.DropTable(
                name: "ChannelGroup");

            migrationBuilder.DropTable(
                name: "LessonChannelMessage");

            migrationBuilder.DropTable(
                name: "QueueItem");

            migrationBuilder.DropTable(
                name: "QueueTake");

            migrationBuilder.DropTable(
                name: "UserDeviceToken");

            migrationBuilder.DropIndex(
                name: "IX_Role_LessonChannelMessageTaskDbModelRealId",
                table: "Role");

            migrationBuilder.DropIndex(
                name: "IX_File_LessonChannelMessageSolutionDbModelRealId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_File_LessonChannelMessageTaskDbModelRealId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_Channel_GroupId",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "JoinTime",
                table: "UserServer");

            migrationBuilder.DropColumn(
                name: "LessonChannelMessageTaskDbModelRealId",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "ServerCanCheckGrades",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "LessonChannelMessageSolutionDbModelRealId",
                table: "File");

            migrationBuilder.DropColumn(
                name: "LessonChannelMessageTaskDbModelRealId",
                table: "File");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "TextLessonChannelDbModel_DeleteTime",
                table: "Channel");
        }
    }
}
