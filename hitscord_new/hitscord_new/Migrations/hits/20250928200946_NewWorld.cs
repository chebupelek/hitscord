using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitscord_new.Migrations
{
    /// <inheritdoc />
    public partial class NewWorld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Server",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IconFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Server", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Mail = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    AccountName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccountTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<int>(type: "integer", nullable: false),
                    AccountCreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notifiable = table.Column<bool>(type: "boolean", nullable: false),
                    FriendshipApplication = table.Column<bool>(type: "boolean", nullable: false),
                    NonFriendMessage = table.Column<bool>(type: "boolean", nullable: false),
                    IconFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotificationLifeTime = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: false),
                    ServerCanChangeRole = table.Column<bool>(type: "boolean", nullable: false),
                    ServerCanWorkChannels = table.Column<bool>(type: "boolean", nullable: false),
                    ServerCanDeleteUsers = table.Column<bool>(type: "boolean", nullable: false),
                    ServerCanMuteOther = table.Column<bool>(type: "boolean", nullable: false),
                    ServerCanDeleteOthersMessages = table.Column<bool>(type: "boolean", nullable: false),
                    ServerCanIgnoreMaxCount = table.Column<bool>(type: "boolean", nullable: false),
                    ServerCanCreateRoles = table.Column<bool>(type: "boolean", nullable: false),
                    ServerCanCreateLessons = table.Column<bool>(type: "boolean", nullable: false),
                    ServerCanCheckAttendance = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Role_Server_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Server",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplyToMessageId = table.Column<long>(type: "bigint", nullable: true),
                    DeleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TaggedUsers = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    MessageType = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    Title = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    IsAnonimous = table.Column<bool>(type: "boolean", nullable: true),
                    Multiple = table.Column<bool>(type: "boolean", nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => new { x.Id, x.ChatId });
                    table.ForeignKey(
                        name: "FK_ChatMessage_Chat_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chat",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChatMessage_User_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Friendship",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserIdFrom = table.Column<Guid>(type: "uuid", nullable: false),
                    UserIdTo = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendship", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Friendship_User_UserIdFrom",
                        column: x => x.UserIdFrom,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Friendship_User_UserIdTo",
                        column: x => x.UserIdTo,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FriendshipApplication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserIdFrom = table.Column<Guid>(type: "uuid", nullable: false),
                    UserIdTo = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendshipApplication", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FriendshipApplication_User_UserIdFrom",
                        column: x => x.UserIdFrom,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FriendshipApplication_User_UserIdTo",
                        column: x => x.UserIdTo,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LastReadChatMessage",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastReadedMessageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LastReadChatMessage", x => new { x.UserId, x.ChatId });
                    table.ForeignKey(
                        name: "FK_LastReadChatMessage_Chat_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LastReadChatMessage_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsReaded = table.Column<bool>(type: "boolean", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TextChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerUserName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerApplications_Server_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Server",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServerApplications_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserChat",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    NonNotifiable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChat", x => new { x.UserId, x.ChatId });
                    table.ForeignKey(
                        name: "FK_UserChat_Chat_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserChat_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserServer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsBanned = table.Column<bool>(type: "boolean", nullable: false),
                    BanReason = table.Column<string>(type: "text", nullable: true),
                    BanTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserServerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NonNotifiable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserServer", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "ChatVoteVariant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    VoteId = table.Column<long>(type: "bigint", nullable: false),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatVoteVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatVoteVariant_ChatMessage_VoteId_ChatId",
                        columns: x => new { x.VoteId, x.ChatId },
                        principalTable: "ChatMessage",
                        principalColumns: new[] { "Id", "ChatId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscribeRole",
                columns: table => new
                {
                    UserServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscribeRole", x => new { x.UserServerId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_SubscribeRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubscribeRole_UserServer_UserServerId",
                        column: x => x.UserServerId,
                        principalTable: "UserServer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatVariantUser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatVariantUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatVariantUser_ChatVoteVariant_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ChatVoteVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatVariantUser_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
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
                    ChannelMessageId = table.Column<long>(type: "bigint", nullable: true),
                    TextChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Channel_Server_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Server",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelCanJoin",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoiceChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelCanJoin", x => new { x.RoleId, x.VoiceChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelCanJoin_Channel_VoiceChannelId",
                        column: x => x.VoiceChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCanJoin_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelCanSee",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelCanSee", x => new { x.RoleId, x.ChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelCanSee_Channel_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCanSee_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelCanUse",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelCanUse", x => new { x.RoleId, x.SubChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelCanUse_Channel_SubChannelId",
                        column: x => x.SubChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCanUse_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelCanWrite",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelCanWrite", x => new { x.RoleId, x.TextChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelCanWrite_Channel_TextChannelId",
                        column: x => x.TextChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCanWrite_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelCanWriteSub",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelCanWriteSub", x => new { x.RoleId, x.TextChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelCanWriteSub_Channel_TextChannelId",
                        column: x => x.TextChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCanWriteSub_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelMessage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TextChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplyToMessageId = table.Column<long>(type: "bigint", nullable: true),
                    DeleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TaggedUsers = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    TaggedRoles = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    MessageType = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    Title = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    IsAnonimous = table.Column<bool>(type: "boolean", nullable: true),
                    Multiple = table.Column<bool>(type: "boolean", nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMessage", x => new { x.Id, x.TextChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelMessage_Channel_TextChannelId",
                        column: x => x.TextChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelMessage_UserServer_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "UserServer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelNotificated",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelNotificated", x => new { x.RoleId, x.NotificationChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelNotificated_Channel_NotificationChannelId",
                        column: x => x.NotificationChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelNotificated_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LastReadChannelMessage",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastReadedMessageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LastReadChannelMessage", x => new { x.UserId, x.TextChannelId });
                    table.ForeignKey(
                        name: "FK_LastReadChannelMessage_Channel_TextChannelId",
                        column: x => x.TextChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LastReadChannelMessage_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NonNotifiableChannel",
                columns: table => new
                {
                    UserServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonNotifiableChannel", x => new { x.UserServerId, x.TextChannelId });
                    table.ForeignKey(
                        name: "FK_NonNotifiableChannel_Channel_TextChannelId",
                        column: x => x.TextChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonNotifiableChannel_UserServer_UserServerId",
                        column: x => x.UserServerId,
                        principalTable: "UserServer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "UserVoiceChannel",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoiceChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    MuteStatus = table.Column<int>(type: "integer", nullable: false),
                    IsStream = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVoiceChannel", x => new { x.UserId, x.VoiceChannelId });
                    table.ForeignKey(
                        name: "FK_UserVoiceChannel_Channel_VoiceChannelId",
                        column: x => x.VoiceChannelId,
                        principalTable: "Channel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserVoiceChannel_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelVoteVariant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    VoteId = table.Column<long>(type: "bigint", nullable: false),
                    TextChannelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelVoteVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelVoteVariant_ChannelMessage_VoteId_TextChannelId",
                        columns: x => new { x.VoteId, x.TextChannelId },
                        principalTable: "ChannelMessage",
                        principalColumns: new[] { "Id", "TextChannelId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "File",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Creator = table.Column<Guid>(type: "uuid", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChannelMessageId = table.Column<long>(type: "bigint", nullable: true),
                    TextChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChatMessageId = table.Column<long>(type: "bigint", nullable: true),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_File", x => x.Id);
                    table.ForeignKey(
                        name: "FK_File_ChannelMessage_ChannelMessageId_TextChannelId",
                        columns: x => new { x.ChannelMessageId, x.TextChannelId },
                        principalTable: "ChannelMessage",
                        principalColumns: new[] { "Id", "TextChannelId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_File_ChatMessage_ChatMessageId_ChatId",
                        columns: x => new { x.ChatMessageId, x.ChatId },
                        principalTable: "ChatMessage",
                        principalColumns: new[] { "Id", "ChatId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_File_Server_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Server",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_File_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
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
                    TimeEnter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeLeave = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "ChannelVariantUser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelVariantUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelVariantUser_ChannelVoteVariant_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ChannelVoteVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelVariantUser_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Channel_ChannelMessageId_TextChannelId",
                table: "Channel",
                columns: new[] { "ChannelMessageId", "TextChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channel_ServerId",
                table: "Channel",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCanJoin_VoiceChannelId",
                table: "ChannelCanJoin",
                column: "VoiceChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCanSee_ChannelId",
                table: "ChannelCanSee",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCanUse_SubChannelId",
                table: "ChannelCanUse",
                column: "SubChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCanWrite_TextChannelId",
                table: "ChannelCanWrite",
                column: "TextChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCanWriteSub_TextChannelId",
                table: "ChannelCanWriteSub",
                column: "TextChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMessage_AuthorId",
                table: "ChannelMessage",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMessage_TextChannelId",
                table: "ChannelMessage",
                column: "TextChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelNotificated_NotificationChannelId",
                table: "ChannelNotificated",
                column: "NotificationChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelVariantUser_UserId",
                table: "ChannelVariantUser",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelVariantUser_VariantId",
                table: "ChannelVariantUser",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelVoteVariant_VoteId_TextChannelId",
                table: "ChannelVoteVariant",
                columns: new[] { "VoteId", "TextChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_AuthorId",
                table: "ChatMessage",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_ChatId",
                table: "ChatMessage",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatVariantUser_UserId",
                table: "ChatVariantUser",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatVariantUser_VariantId",
                table: "ChatVariantUser",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatVoteVariant_VoteId_ChatId",
                table: "ChatVoteVariant",
                columns: new[] { "VoteId", "ChatId" });

            migrationBuilder.CreateIndex(
                name: "IX_File_ChannelMessageId_TextChannelId",
                table: "File",
                columns: new[] { "ChannelMessageId", "TextChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_File_ChatMessageId_ChatId",
                table: "File",
                columns: new[] { "ChatMessageId", "ChatId" });

            migrationBuilder.CreateIndex(
                name: "IX_File_ServerId",
                table: "File",
                column: "ServerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_File_UserId",
                table: "File",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendship_UserIdFrom_UserIdTo",
                table: "Friendship",
                columns: new[] { "UserIdFrom", "UserIdTo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendship_UserIdTo",
                table: "Friendship",
                column: "UserIdTo");

            migrationBuilder.CreateIndex(
                name: "IX_FriendshipApplication_UserIdFrom_UserIdTo",
                table: "FriendshipApplication",
                columns: new[] { "UserIdFrom", "UserIdTo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FriendshipApplication_UserIdTo",
                table: "FriendshipApplication",
                column: "UserIdTo");

            migrationBuilder.CreateIndex(
                name: "IX_LastReadChannelMessage_TextChannelId",
                table: "LastReadChannelMessage",
                column: "TextChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_LastReadChatMessage_ChatId",
                table: "LastReadChatMessage",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_NonNotifiableChannel_TextChannelId",
                table: "NonNotifiableChannel",
                column: "TextChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Role_ServerId",
                table: "Role",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerApplications_ServerId",
                table: "ServerApplications",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerApplications_UserId",
                table: "ServerApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscribeRole_RoleId",
                table: "SubscribeRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChat_ChatId",
                table: "UserChat",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_UserServer_ServerId",
                table: "UserServer",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserServer_UserId",
                table: "UserServer",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVoiceChannel_UserId",
                table: "UserVoiceChannel",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserVoiceChannel_VoiceChannelId",
                table: "UserVoiceChannel",
                column: "VoiceChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_ChannelMessage_ChannelMessageId_TextChannelId",
                table: "Channel",
                columns: new[] { "ChannelMessageId", "TextChannelId" },
                principalTable: "ChannelMessage",
                principalColumns: new[] { "Id", "TextChannelId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_ChannelMessage_ChannelMessageId_TextChannelId",
                table: "Channel");

            migrationBuilder.DropTable(
                name: "ChannelCanJoin");

            migrationBuilder.DropTable(
                name: "ChannelCanSee");

            migrationBuilder.DropTable(
                name: "ChannelCanUse");

            migrationBuilder.DropTable(
                name: "ChannelCanWrite");

            migrationBuilder.DropTable(
                name: "ChannelCanWriteSub");

            migrationBuilder.DropTable(
                name: "ChannelNotificated");

            migrationBuilder.DropTable(
                name: "ChannelVariantUser");

            migrationBuilder.DropTable(
                name: "ChatVariantUser");

            migrationBuilder.DropTable(
                name: "File");

            migrationBuilder.DropTable(
                name: "Friendship");

            migrationBuilder.DropTable(
                name: "FriendshipApplication");

            migrationBuilder.DropTable(
                name: "LastReadChannelMessage");

            migrationBuilder.DropTable(
                name: "LastReadChatMessage");

            migrationBuilder.DropTable(
                name: "NonNotifiableChannel");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PairDbModelRoleDbModel");

            migrationBuilder.DropTable(
                name: "PairUser");

            migrationBuilder.DropTable(
                name: "ServerApplications");

            migrationBuilder.DropTable(
                name: "SubscribeRole");

            migrationBuilder.DropTable(
                name: "UserChat");

            migrationBuilder.DropTable(
                name: "UserVoiceChannel");

            migrationBuilder.DropTable(
                name: "ChannelVoteVariant");

            migrationBuilder.DropTable(
                name: "ChatVoteVariant");

            migrationBuilder.DropTable(
                name: "Pair");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "ChatMessage");

            migrationBuilder.DropTable(
                name: "Chat");

            migrationBuilder.DropTable(
                name: "ChannelMessage");

            migrationBuilder.DropTable(
                name: "Channel");

            migrationBuilder.DropTable(
                name: "UserServer");

            migrationBuilder.DropTable(
                name: "Server");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
