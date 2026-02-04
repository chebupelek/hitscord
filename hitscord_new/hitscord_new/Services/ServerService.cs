using Authzed.Api.V0;
using Grpc.Core;
using Grpc.Net.Client.Balancer;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.nClamUtil;
using hitscord.Utils;
using hitscord.WebSockets;
using hitscord_new.Migrations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using nClam;
using NickBuhro.Translit;
using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace hitscord.Services;

//_logger.LogInformation("applicationId {applicationId}", applicationId);

public class ServerService : IServerService
{
    private readonly HitsContext _hitsContext;
	private readonly IAuthorizationService _authorizationService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly nClamService _clamService;
	private readonly MinioService _minioService;
	//private readonly ILogger<ServerService> _logger;

	public ServerService(/*ILogger<ServerService> logger, */HitsContext hitsContext, IAuthorizationService authorizationService, WebSocketsManager webSocketManager, INotificationService notificationsService, nClamService clamService, MinioService minioService)
	{
		//_logger = logger;
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
		_minioService = minioService ?? throw new ArgumentNullException(nameof(minioService));
	}

    public async Task<ServerDbModel> CheckServerExistAsync(Guid serverId, bool includeChannels)
    {
        var server = includeChannels ? await _hitsContext.Server.Include(s => s.Roles).Include(s => s.Channels).FirstOrDefaultAsync(s => s.Id == serverId) :
            await _hitsContext.Server.Include(s => s.Roles).FirstOrDefaultAsync(s => s.Id == serverId);
        if (server == null)
        {
            throw new CustomException("Server not found", "Check server for existing", "Server id", 404, "Сервер не найден", "Проверка наличия сервера");
        }
        return server;
    }

	public async Task<ServerDbModel> GetServerFullModelAsync(Guid serverId)
    {
        var server = await _hitsContext.Server
                .Include(s => s.Channels)
                .Include(s => s.Roles)
				.Include(s => s.Invitations)
                .FirstOrDefaultAsync(s => s.Id == serverId);
        if (server == null)
        {
            throw new CustomException("Server not found", "Get server with full model", "Server id", 404, "Сервер не найден", "Получение полной информации о сервере");
        }
        return server;
    }

    private async Task<RoleDbModel> CreateRoleAsync(Guid serverId, RoleEnum role, string roleName, string color, bool ServerCanChangeRole, bool ServerCanWorkChannels, bool ServerCanDeleteUsers, bool ServerCanMuteOther, bool ServerCanDeleteOthersMessages, bool ServerCanIgnoreMaxCount, bool ServerCanCreateRoles, bool ServerCanCreateLessons, bool ServerCanCheckAttendance, bool ServerCanUseInvitations)
    {
        var newRole = new RoleDbModel()
        {
            Name = roleName,
            Role = role,
            ServerId = serverId,
            Color = color,
            Tag = Regex.Replace(Transliteration.CyrillicToLatin(roleName, Language.Russian), "[^a-zA-Z0-9]", "").ToLower(),
			ServerCanChangeRole = ServerCanChangeRole,
			ServerCanWorkChannels = ServerCanWorkChannels,
			ServerCanDeleteUsers = ServerCanDeleteUsers,
			ServerCanMuteOther = ServerCanMuteOther,
			ServerCanDeleteOthersMessages = ServerCanDeleteOthersMessages,
			ServerCanIgnoreMaxCount = ServerCanIgnoreMaxCount,
			ServerCanCreateRoles = ServerCanCreateRoles,
			ServerCanCreateLessons = ServerCanCreateLessons,
			ServerCanCheckAttendance = ServerCanCheckAttendance,
			ServerCanUseInvitations = ServerCanUseInvitations,
			ChannelCanSee = new List<ChannelCanSeeDbModel>(),
			ChannelCanWrite = new List<ChannelCanWriteDbModel>(),
			ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>(),
			ChannelNotificated = new List<ChannelNotificatedDbModel>(),
			ChannelCanUse = new List<ChannelCanUseDbModel>(),
			ChannelCanJoin = new List<ChannelCanJoinDbModel>(),
		};
        await _hitsContext.Role.AddAsync(newRole);
        await _hitsContext.SaveChangesAsync();
        return newRole;
    }

	public async Task<FileMetaResponseDTO?> GetImageAsync(Guid iconId)
	{
		var file = await _hitsContext.File.FindAsync(iconId);
		if (file == null)
			return null;

		if (!file.Type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			return null;

		return new FileMetaResponseDTO
		{
			FileId = file.Id,
			FileName = file.Name,
			FileType = file.Type,
			FileSize = file.Size,
			Deleted = file.Deleted
		};
	}

	public async Task<ServerIdDTO> CreateServerAsync(string token, string serverName, ServerTypeEnum? type)
    {
        var user = await _authorizationService.GetUserAsync(token);

		if (user.SystemRoles.Count == 0)
		{
			throw new CustomException("User cant create servers", "Create server", "User", 401, "Пользователь не имеет права создавать сервера", "Создание сервера");
		}

		if (!(user.SystemRoles.Any(sr => sr.Type == SystemRoleTypeEnum.Teacher)) && type != null && type == ServerTypeEnum.Teacher)
		{
			throw new CustomException("User cant create teachers servers", "Create server", "User", 401, "Пользователь не имеет права создавать учебные сервера", "Создание сервера");
		}

        var newServer = new ServerDbModel()
        {
            Name = serverName,
			IsClosed = false,
			Roles = new List<RoleDbModel>(),
			Channels = new List<ChannelDbModel>(),
			Subscribtions = new List<UserServerDbModel>(),
			ServerType = (type != null && type == ServerTypeEnum.Teacher) ? ServerTypeEnum.Teacher : ServerTypeEnum.Student
		};

		await _hitsContext.Server.AddAsync(newServer);
		await _hitsContext.SaveChangesAsync();

		var creatorRole = await CreateRoleAsync(newServer.Id, RoleEnum.Creator, "Владелец", "#FF0000", true, true, true, true, true, true, true, true, true, true);
        var adminRole = await CreateRoleAsync(newServer.Id, RoleEnum.Admin, "Администратор", "#00FF00", true, true, true, true, true, true, true, true, true, true);
        var uncertainRole = await CreateRoleAsync(newServer.Id, RoleEnum.Uncertain, "Базовая", "#FFFF00", false, false, false, false, false, false, false, false, false, false);
        newServer.Roles = new List<RoleDbModel> { creatorRole, adminRole, uncertainRole };
        _hitsContext.Server.Update(newServer);
        await _hitsContext.SaveChangesAsync();

		var newSub = new UserServerDbModel
		{
			Id = Guid.NewGuid(),
			UserId = user.Id,
			ServerId = newServer.Id,
			UserServerName = user.AccountName,
			IsBanned = false,
			NonNotifiable = false,
			SubscribeRoles = new List<SubscribeRoleDbModel>()
		};

		newSub.SubscribeRoles.Add(new SubscribeRoleDbModel
		{
			UserServerId = newSub.Id,
			RoleId = creatorRole.Id
		});
		await _hitsContext.UserServer.AddAsync(newSub);
        await _hitsContext.SaveChangesAsync();

        var newTextChannel = new TextChannelDbModel
        {
            Name = "Основной текстовый",
            ServerId = newServer.Id,
			ChannelCanSee = new List<ChannelCanSeeDbModel>(),
			Messages = new List<ChannelMessageDbModel>(),
			ChannelCanWrite = new List<ChannelCanWriteDbModel>(),
			ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>(),
		};
		foreach (var item in new[]
		{
			new ChannelCanSeeDbModel { ChannelId = newTextChannel.Id, RoleId = creatorRole.Id },
			new ChannelCanSeeDbModel { ChannelId = newTextChannel.Id, RoleId = adminRole.Id },
			new ChannelCanSeeDbModel { ChannelId = newTextChannel.Id, RoleId = uncertainRole.Id }
		})
		{
			newTextChannel.ChannelCanSee.Add(item);
		}
		foreach (var item in new[]
		{
			new ChannelCanWriteDbModel { TextChannelId = newTextChannel.Id, RoleId = creatorRole.Id },
			new ChannelCanWriteDbModel { TextChannelId = newTextChannel.Id, RoleId = adminRole.Id },
			new ChannelCanWriteDbModel { TextChannelId = newTextChannel.Id, RoleId = uncertainRole.Id }
		})
		{
			newTextChannel.ChannelCanWrite.Add(item);
		}
		foreach (var item in new[]
		{
			new ChannelCanWriteSubDbModel { TextChannelId = newTextChannel.Id, RoleId = creatorRole.Id },
			new ChannelCanWriteSubDbModel { TextChannelId = newTextChannel.Id, RoleId = adminRole.Id },
			new ChannelCanWriteSubDbModel { TextChannelId = newTextChannel.Id, RoleId = uncertainRole.Id }
		})
		{
			newTextChannel.ChannelCanWriteSub.Add(item);
		}

		var newVoiceChannel = new VoiceChannelDbModel
        {
			Name = "Основной голосовой",
			ServerId = newServer.Id,
			ChannelCanSee = new List<ChannelCanSeeDbModel>(),
			MaxCount = 999,
			ChannelCanJoin = new List<ChannelCanJoinDbModel>()
		};
		foreach (var item in new[]
		{
			new ChannelCanSeeDbModel { ChannelId = newVoiceChannel.Id, RoleId = creatorRole.Id },
			new ChannelCanSeeDbModel { ChannelId = newVoiceChannel.Id, RoleId = adminRole.Id },
			new ChannelCanSeeDbModel { ChannelId = newVoiceChannel.Id, RoleId = uncertainRole.Id }
		})
		{
			newVoiceChannel.ChannelCanSee.Add(item);
		}
		foreach (var item in new[]
		{
			new ChannelCanJoinDbModel { VoiceChannelId = newVoiceChannel.Id, RoleId = creatorRole.Id },
			new ChannelCanJoinDbModel { VoiceChannelId = newVoiceChannel.Id, RoleId = adminRole.Id },
			new ChannelCanJoinDbModel { VoiceChannelId = newVoiceChannel.Id, RoleId = uncertainRole.Id }
		})
		{
			newVoiceChannel.ChannelCanJoin.Add(item);
		}
		
		await _hitsContext.Channel.AddAsync(newTextChannel);
        await _hitsContext.Channel.AddAsync(newVoiceChannel);
        await _hitsContext.SaveChangesAsync();

		newServer.Channels.Add(newTextChannel);
        newServer.Channels.Add(newVoiceChannel);
        _hitsContext.Server.Update(newServer);
        await _hitsContext.SaveChangesAsync();

		var lastReadedMessage = new LastReadChannelMessageDbModel
		{
			UserId = user.Id,
			TextChannelId = newTextChannel.Id,
			LastReadedMessageId = 0
		};

		_hitsContext.LastReadChannelMessage.Add(lastReadedMessage);
		await _hitsContext.SaveChangesAsync();

		return (new ServerIdDTO { ServerId = newServer.Id });
    }

	public async Task SubscribeAsync(string token, string invitationToken, string? userName)
	{
		var user = await _authorizationService.GetUserAsync(token);/*
		if (user.SystemRoles.Count == 0)
		{
			throw new CustomException("User cant subscribe to servers", "Subscribe", "User", 403, "Пользователь не имеет права присоединяться к серверам", "Подписка");
		}*/
		var serverInvitation = await _hitsContext.Invitation.FirstOrDefaultAsync(i => i.Token == invitationToken && i.IsRevoked == false && i.ExpiresAt > DateTime.UtcNow);
		if (serverInvitation == null)
		{
			throw new CustomException("Invitation not found", "Check invitation is exist", "Invitation", 404, "Приглашение не найдено", "Подписка");
		}
		var server = await CheckServerExistAsync(serverInvitation.ServerId, true);
		var existedSub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == serverInvitation.ServerId);
		if (existedSub != null && existedSub.IsBanned == true)
		{
			throw new CustomException("User banned in this server", "Subscribe", "User", 401, "Пользователь забанен на этом сервере", "Подписка");
		}
		if (existedSub != null)
		{
			throw new CustomException("User is already subscribed to this server", "Check subscription is not exist", "User", 400, "Пользователь уже является участником этого сервера", "Подписка");
		}
		if (server.IsClosed == false)
		{
			var uncertainRole = server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain);
			if (uncertainRole == null)
			{
				throw new CustomException("Uncertain role not found", "Check uncertain role is exist", "uncertainRole", 400, "Неопределенная роль не нйдена", "Подписка");
			}
			var newSub = new UserServerDbModel
			{
				Id = Guid.NewGuid(),
				UserId = user.Id,
				ServerId = server.Id,
				InvitationId = serverInvitation.Id,
				UserServerName = user.AccountName,
				IsBanned = false,
				NonNotifiable = false,
				SubscribeRoles = new List<SubscribeRoleDbModel>()
			};
			newSub.SubscribeRoles.Add(new SubscribeRoleDbModel
			{
				UserServerId = newSub.Id,
				RoleId = uncertainRole.Id
			});

			var channelsCanRead = await _hitsContext.ChannelCanSee.Include(ccs => ccs.Channel).Where(ccs => (ccs.Channel is TextChannelDbModel || ccs.Channel is NotificationChannelDbModel || ccs.Channel is SubChannelDbModel) && ccs.Channel.ServerId == server.Id && ccs.RoleId == uncertainRole.Id).Select(ccs => ccs.ChannelId).ToListAsync();
			var lastReadedList = new List<LastReadChannelMessageDbModel>();
			if (channelsCanRead != null)
			{
				foreach (var channel in channelsCanRead)
				{
					lastReadedList.Add(new LastReadChannelMessageDbModel
					{
						UserId = user.Id,
						TextChannelId = channel,
						LastReadedMessageId = (await _hitsContext.ChannelMessage.Where(cm => cm.TextChannelId == channel).Select(m => (long?)m.Id).MaxAsync() ?? 0)
					});
				}
			}

			await _hitsContext.UserServer.AddAsync(newSub);
			await _hitsContext.LastReadChannelMessage.AddRangeAsync(lastReadedList);
			await _hitsContext.SaveChangesAsync();

			var newSubscriberResponse = new ServerUserDTO
			{
				ServerId = server.Id,
				UserId = user.Id,
				UserName = user.AccountName,
				UserTag = user.AccountTag,
				Icon = null,
				Roles = new List<UserServerRoles>{
					new UserServerRoles
					{
						RoleId = uncertainRole.Id,
						RoleName = uncertainRole.Name,
						RoleType = uncertainRole.Role
					}
				},
				Notifiable = user.Notifiable,
				FriendshipApplication = user.FriendshipApplication,
				NonFriendMessage = user.NonFriendMessage,
				isFriend = false,
				SystemRoles = user.SystemRoles
					.Select(sr => new SystemRoleShortItemDTO
					{
						Id = null,
						Name = sr.Name,
						Type = sr.Type
					})
					.ToList()
			};
			if (user != null && user.IconFileId != null)
			{
				var userIcon = await GetImageAsync((Guid)user.IconFileId);
				newSubscriberResponse.Icon = userIcon;
			}

			var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
			var allFriends = await _hitsContext.Friendship
				.Where(f => f.UserIdFrom == user.Id || f.UserIdTo == user.Id)
				.Select(f => f.UserIdFrom == user.Id ? f.UserIdTo : f.UserIdFrom)
				.Distinct()
				.ToListAsync();
			var friendsSet = new HashSet<Guid>(allFriends);
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				foreach (var alertedUser in alertedUsers)
				{
					newSubscriberResponse.isFriend = friendsSet.Contains(alertedUser);
					await _webSocketManager.BroadcastMessageAsync(newSubscriberResponse, new List<Guid> { alertedUser }, "New user on server");
				}
			}
		}
		else
		{
			var application = await _hitsContext.ServerApplications.FirstOrDefaultAsync(sa => sa.UserId == user.Id && sa.ServerId == server.Id);
			if (application != null)
			{
				throw new CustomException("Application already exist", "Subscribe", "User", 400, "Заявка на подписку уже существует", "Подписка");
			}

			var newApplication = new ServerApplicationDbModel
			{
				UserId = user.Id,
				ServerId = server.Id,
				InvitationId = serverInvitation.Id,
				ServerUserName = userName,
				CreatedAt = DateTime.UtcNow
			};

			await _hitsContext.ServerApplications.AddAsync(newApplication);
			await _hitsContext.SaveChangesAsync();

			await _webSocketManager.BroadcastMessageAsync(server.Id, new List<Guid> { user.Id }, "Server application created");
		}
	}

	public async Task UnsubscribeAsync(Guid serverId, string token)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);

		var sub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);
		if (sub == null)
		{
			throw new CustomException("User not subscriber of this server", "Check subscription is exist", "User", 401, "Пользователь не является участником этого сервера", "Отписка");
		}
		if (sub.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator))
		{
			throw new CustomException("User is creator of this server", "Check subscription roles", "User", 401, "Пользователь - создатель сервера", "Отписка");
		}

		var userVoiceChannel = await _hitsContext.UserVoiceChannel
			.Include(us => us.VoiceChannel)
			.FirstOrDefaultAsync(us =>
				us.VoiceChannel.ServerId == server.Id
				&& us.UserId == user.Id);
		if (userVoiceChannel != null)
		{
			_hitsContext.UserVoiceChannel.Remove(userVoiceChannel);
		}

		var lastMessage = await _hitsContext.LastReadChannelMessage.Include(lr => lr.TextChannel).Where(lr => lr.UserId == user.Id && lr.TextChannel.ServerId == server.Id).ToListAsync();
		_hitsContext.LastReadChannelMessage.RemoveRange(lastMessage);

		var nonNitifiables = await _hitsContext.NonNotifiableChannel.Where(nnc => nnc.UserServerId == sub.Id).ToListAsync();
		_hitsContext.NonNotifiableChannel.RemoveRange(nonNitifiables);

		_hitsContext.UserServer.Remove(sub);

		await _hitsContext.SaveChangesAsync();

		var newUnsubscriberResponse = new UnsubscribeResponseDTO
		{
			ServerId = serverId,
			UserId = user.Id,
		};
		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, alertedUsers, "User unsubscribe");
		}
	}

	public async Task UnsubscribeForCreatorAsync(Guid serverId, string token, Guid newCreatorId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		var newCreator = await _authorizationService.GetUserAsync(newCreatorId);
		if (server.ServerType == ServerTypeEnum.Teacher && !(newCreator.SystemRoles.Any(sr => sr.Type == SystemRoleTypeEnum.Teacher)))
		{
			throw new CustomException("New creator isnt teacher", "Check subscription is exist", "User", 401, "Новый владелец не является учителем", "Отписка для создателя");
		}

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner not subscriber of this server", "Check subscription is exist", "User", 401, "Владелец не является участником этого сервера", "Отписка для создателя");
		}
		if (!ownerSub.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator))
		{
			throw new CustomException("User is not creator of this server", "Check subscription roles", "User", 401, "Пользователь - не создатель сервера", "Отписка для создателя");
		}

		var newCreatorSub = await _hitsContext.UserServer
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == newCreator.Id);
		if (newCreatorSub == null)
		{
			throw new CustomException("User not subscriber of this server", "Check subscription is exist", "User", 401, "Пользователь не является участником этого сервера", "Отписка для создателя");
		}

		var creatorRole = server.Roles.FirstOrDefault(s => s.Role == RoleEnum.Creator);
		if (creatorRole == null)
		{
			throw new CustomException("Role not found", "Unsubscribe for creator", "Creator role", 404, "Роль создания не найдена", "Отписка для создателя");
		}

		var userVoiceChannel = await _hitsContext.UserVoiceChannel
			.Include(us => us.VoiceChannel)
			.FirstOrDefaultAsync(us =>
				us.VoiceChannel.ServerId == server.Id
				&& us.UserId == owner.Id);
		if (userVoiceChannel != null)
		{
			_hitsContext.UserVoiceChannel.Remove(userVoiceChannel);
		}

		var lastMessage = await _hitsContext.LastReadChannelMessage.Include(lr => lr.TextChannel).Where(lr => lr.UserId == owner.Id && lr.TextChannel.ServerId == server.Id).ToListAsync();
		_hitsContext.LastReadChannelMessage.RemoveRange(lastMessage);

		var nonNitifiables = await _hitsContext.NonNotifiableChannel.Where(nnc => nnc.UserServerId == ownerSub.Id).ToListAsync();
		_hitsContext.NonNotifiableChannel.RemoveRange(nonNitifiables);

		_hitsContext.UserServer.Remove(ownerSub);

		newCreatorSub.SubscribeRoles.Clear();
		newCreatorSub.SubscribeRoles.Add(new SubscribeRoleDbModel
		{
			UserServerId = newCreatorSub.Id,
			RoleId = creatorRole.Id
		});

		_hitsContext.UserServer.Update(newCreatorSub);
		await _hitsContext.SaveChangesAsync();

		var newUnsubscriberResponse = new UnsubscribeResponseDTO
		{
			ServerId = serverId,
			UserId = owner.Id,
		};
		var newUserRole = new NewUserRoleResponseDTO
		{
			ServerId = serverId,
			UserId = newCreatorSub.UserId,
			RoleId = creatorRole.Id,
		};
		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, alertedUsers, "User unsubscribe");
			await _webSocketManager.BroadcastMessageAsync(newUserRole, alertedUsers, "Role changed");
		}
	}

	public async Task DeleteServerAsync(Guid serverId, string token)
    {
        var owner = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, true);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner not subscriber of this server", "Check subscription is exist", "User", 401, "Владелец не является участником этого сервера", "Удаление сервера");
		}
		if (!ownerSub.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator))
		{
			throw new CustomException("User is not creator of this server", "Check subscription roles", "User", 401, "Пользователь - не создатель сервера", "Удаление сервера");
		}

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();

		var userServerRelations = _hitsContext.UserServer.Where(us => us.ServerId == server.Id);
        var serverRoles = _hitsContext.Role.Where(r => r.ServerId == server.Id);

		var lastMessage = await _hitsContext.LastReadChannelMessage.Include(lr => lr.TextChannel).Where(lr => lr.TextChannel.ServerId == server.Id).ToListAsync();
		_hitsContext.LastReadChannelMessage.RemoveRange(lastMessage);

		var nonNitifiables = await _hitsContext.NonNotifiableChannel.Include(nnc => nnc.UserServer).Where(nnc => nnc.UserServer.ServerId == server.Id).ToListAsync();
		_hitsContext.NonNotifiableChannel.RemoveRange(nonNitifiables);

		_hitsContext.UserServer.RemoveRange(userServerRelations);
        _hitsContext.Role.RemoveRange(serverRoles);

        var voiceChannels = server.Channels.OfType<VoiceChannelDbModel>().ToList();
		var pairVoiceChannels = server.Channels.OfType<PairVoiceChannelDbModel>().ToList();
		foreach (var voiceChannel in voiceChannels)
        {
            voiceChannel.Users.Clear();
        }
		foreach (var pairVoiceChannel in pairVoiceChannels)
		{
			pairVoiceChannel.Users.Clear();
		}
		var channelsToDelete = server.Channels.ToList();
		var applications = await _hitsContext.ServerApplications.Where(sa => sa.ServerId == server.Id).ToListAsync();
        _hitsContext.Channel.RemoveRange(channelsToDelete);
		_hitsContext.ServerApplications.RemoveRange(applications);

		if (server.IconFileId != null)
		{
			var iconFile = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == server.IconFileId);
			if (iconFile != null)
			{
				try
				{
					await _minioService.DeleteFileAsync(iconFile.Path.TrimStart('/'));
				}
				catch (Exception ex)
				{
				}

				// Удаляем запись из базы
				_hitsContext.File.Remove(iconFile);
			}
		}
		
		_hitsContext.Server.Remove(server);
        await _hitsContext.SaveChangesAsync();

		await _hitsContext.ChannelMessage
			.Where(m => m.TextChannel.ServerId == server.Id)
			.ExecuteUpdateAsync(setters => setters
				.SetProperty(m => m.DeleteTime, _ => DateTime.UtcNow.AddDays(21)));

		var serverDelete = new ServerDeleteDTO
        {
			ServerName = server.Name,
			ServerId = serverId
        };
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {
			await _webSocketManager.BroadcastMessageAsync(serverDelete, alertedUsers, "Server deleted");
		}
	}

    public async Task<ServersListDTO> GetServerListAsync(string token)
    {
        var user = await _authorizationService.GetUserAsync(token);

		var subscriptions = await _hitsContext.UserServer
			.Include(us => us.Server)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.Where(us => us.UserId == user.Id).ToListAsync();

		var serverList = new List<ServersListItemDTO>();

		foreach (var sub in subscriptions)
		{
			var icon = sub.Server.IconFileId == null ? null : await GetImageAsync((Guid)sub.Server.IconFileId);

			var userRoleIdsForServer = sub.SubscribeRoles.Select(sr => sr.RoleId).ToHashSet();

			var channelIds = await _hitsContext.TextChannel
				.Where(c => c.ServerId == sub.ServerId && EF.Property<string>(c, "ChannelType") == "Text")
				.Select(c => c.Id)
				.ToListAsync();

			var lastReads = await _hitsContext.LastReadChannelMessage
				.Where(lr => lr.UserId == user.Id && channelIds.Contains(lr.TextChannelId))
				.ToListAsync();

			var nonReadedMessages = 0;
			var nonReadedTaggedMessages = 0;

			var lastReadsDict = lastReads.ToDictionary(lr => lr.TextChannelId, lr => lr.LastReadedMessageId);

			var nonReadedMessagesQuery = _hitsContext.ChannelMessage
				.Where(cm => lastReadsDict.Keys.Contains((Guid)cm.TextChannelId))
				.AsEnumerable()
				.Where(cm => cm.Id > lastReadsDict[(Guid)cm.TextChannelId] && cm.DeleteTime == null)
				.ToList();

			nonReadedMessages = nonReadedMessagesQuery.Count();
			nonReadedTaggedMessages = nonReadedMessagesQuery.Count(m =>
				m.TaggedUsers.Contains(user.Id) || m.TaggedRoles.Any(rid => userRoleIdsForServer.Contains(rid))
			);

			serverList.Add(new ServersListItemDTO
			{
				ServerId = sub.Server.Id,
				ServerName = sub.Server.Name,
				IsNotifiable = sub.NonNotifiable,
				Icon = icon,
				NonReadedCount = nonReadedMessages,
				NonReadedTaggedCount = nonReadedTaggedMessages,
				ServerType = sub.Server.ServerType
			});
		}

		return (new ServersListDTO
        {
            ServersList = serverList
		});
    }

    public async Task AddRoleToUserAsync(string token, Guid serverId, Guid userId, Guid roleId)
    {
        var owner = await _authorizationService.GetUserAsync(token);
		var user = await _authorizationService.GetUserAsync(userId);
		
		var server = await CheckServerExistAsync(serverId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Check owner", "Owner", 404, "Владелец не найден", "Добавление роли пользователю");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanChangeRole) == false)
		{
			throw new CustomException("Owner does not have rights to change roles", "Check user rights to change roles", "Owner", 403, "Владелец не имеет права изменять роли", "Добавление роли пользователю");
		}

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(us => us.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Check user", "User", 404, "Пользователь не найден", "Добавление роли пользователю");
		}

		if(ownerSub.SubscribeRoles.Min(sr => sr.Role.Role) > userSub.SubscribeRoles.Min(sr => sr.Role.Role))
		{
			throw new CustomException("Owner lower in ierarchy than changed user", "Change user role", "Changed user role", 401, "Пользователь ниже по иерархии чем изменяемый пользователь", "Добавление роли пользователю");
		}

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == serverId && r.Role != RoleEnum.Creator);
		if(role == null)
		{
			throw new CustomException("Role not found", "Change user role", "Role ID", 404, "Роль не найдена", "Добавление роли пользователю");
		}
		if (ownerSub.SubscribeRoles.Min(sr => sr.Role.Role) > role.Role)
		{
			throw new CustomException("Owner lower in ierarchy than added role", "Change role", "Changed user role", 401, "Пользователь ниже по иерархии чем назначаемая роль", "Добавление роли пользователю");
		}

		if (userSub.SubscribeRoles.FirstOrDefault(usr => usr.RoleId == role.Id) != null)
		{
			throw new CustomException("User already has this role", "Change role", "Changed user role", 401, "Пользователь уже имеет эту роль", "Добавление роли пользователю");
		}

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();

		if (role.Role == RoleEnum.Uncertain)
		{
			var rolesIds = userSub.SubscribeRoles.Select(sr => sr.RoleId).ToList();

			var removedChannels = await _hitsContext.ChannelCanSee
				.Where(ccs => rolesIds.Contains(ccs.RoleId))
				.Select(ccs => ccs.ChannelId)
				.ToListAsync();

			var lastRead = await _hitsContext.LastReadChannelMessage
				.Where(lr => lr.UserId == user.Id && removedChannels.Contains(lr.TextChannelId))
				.ToListAsync();

			if (lastRead != null)
			{
				_hitsContext.LastReadChannelMessage.RemoveRange(lastRead);
				await _hitsContext.SaveChangesAsync();
			}

			_hitsContext.SubscribeRole.RemoveRange(userSub.SubscribeRoles);
			await _hitsContext.SaveChangesAsync();

			foreach (var remRole in rolesIds)
			{
				var oldUserRole = new NewUserRoleResponseDTO
				{
					ServerId = serverId,
					UserId = userId,
					RoleId = remRole,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(oldUserRole, alertedUsers, "Role removed from user");
				}
			}
		}
		else
		{
			var unc = userSub.SubscribeRoles.FirstOrDefault(sr => sr.Role.Role == RoleEnum.Uncertain);
			if (unc != null)
			{
				var removedChannels = await _hitsContext.ChannelCanSee
					.Where(ccs => ccs.RoleId == unc.RoleId)
					.Select(ccs => ccs.ChannelId)
					.ToListAsync();

				var lastRead = await _hitsContext.LastReadChannelMessage
					.Where(lr => lr.UserId == user.Id && removedChannels.Contains(lr.TextChannelId))
					.ToListAsync();

				if (lastRead != null)
				{
					_hitsContext.LastReadChannelMessage.RemoveRange(lastRead);
					await _hitsContext.SaveChangesAsync();
				}

				_hitsContext.SubscribeRole.Remove(unc);
				await _hitsContext.SaveChangesAsync();

				var oldUserRole = new NewUserRoleResponseDTO
				{
					ServerId = serverId,
					UserId = userId,
					RoleId = unc.RoleId,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(oldUserRole, alertedUsers, "Role removed from user");
				}
			}
		}

		userSub.SubscribeRoles.Add(new SubscribeRoleDbModel
		{
			UserServerId = userSub.Id,
			RoleId = role.Id
		});

		_hitsContext.UserServer.Update(userSub);
        await _hitsContext.SaveChangesAsync();

		var visibleChannels = await _hitsContext.ChannelCanSee
			.Where(ccs => ccs.RoleId == role.Id)
			.Select(ccs => ccs.ChannelId)
			.ToListAsync();
		foreach (var channel in visibleChannels)
		{
			bool alreadyExists = await _hitsContext.LastReadChannelMessage
				.AnyAsync(lr => lr.UserId == user.Id && lr.TextChannelId == channel);

			if (!alreadyExists)
			{
				var lastMessageId = await _hitsContext.ChannelMessage
					.Where(m => m.TextChannelId == channel)
					.OrderByDescending(m => m.Id)
					.Select(m => (long?)m.Id)
					.FirstOrDefaultAsync() ?? 0;

				var lastRead = new LastReadChannelMessageDbModel
				{
					UserId = user.Id,
					TextChannelId = channel,
					LastReadedMessageId = lastMessageId
				};

				await _hitsContext.LastReadChannelMessage.AddAsync(lastRead);
			}
		}
		await _hitsContext.SaveChangesAsync();

		var newUserRole = new NewUserRoleResponseDTO
		{
			ServerId = serverId,
			UserId = userId,
			RoleId = role.Id,
		};
		if (alertedUsers != null && alertedUsers.Count() > 0)
        {
			await _webSocketManager.BroadcastMessageAsync(newUserRole, alertedUsers, "Role added to user");
        }
    }

	public async Task RemoveRoleFromUserAsync(string token, Guid serverId, Guid userId, Guid roleId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var user = await _authorizationService.GetUserAsync(userId);

		var server = await CheckServerExistAsync(serverId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Check owner sub", "Owner sub", 404, "Владелец не является подписчиком сервера", "Удаление роли у пользователя");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanChangeRole) == false)
		{
			throw new CustomException("Owner does not have rights to change roles", "Check user rights to change roles", "Owner rights", 403, "Владелец не имеет права изменять роли", "Удаление роли у пользователя");
		}

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Check user sub", "User sub", 404, "Пользователь не является подписчиком сервера", "Удаление роли у пользователя");
		}

		if (ownerSub.SubscribeRoles.Min(sr => sr.Role.Role) > userSub.SubscribeRoles.Min(sr => sr.Role.Role))
		{
			throw new CustomException("Owner lower in ierarchy than changed user", "User and Owner roles comparasion", "Changed user role", 401, "Вы ниже по роли чеи пользователь у которого удаляется роль", "Удаление роли у пользователя");
		}

		var deletedRole = userSub.SubscribeRoles.FirstOrDefault(usr => usr.RoleId == roleId);

		if (deletedRole == null)
		{
			throw new CustomException("Deleted role not found", "Deleted role check", "Deleted role", 404, "Удаляемая роль не найдена", "Удаление роли у пользователя");
		}

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (userSub.SubscribeRoles.Count() == 1)
		{
			var uncertainRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.ServerId == server.Id && r.Role == RoleEnum.Uncertain);
			if (uncertainRole == null)
			{
				throw new CustomException("Uncertain role not found", "Uncertain role check", "Uncertain role", 404, "Неопредленная роль не найдена", "Удаление роли у пользователя");
			}

			if (uncertainRole.Id == deletedRole.RoleId)
			{
				throw new CustomException("Only role of user - is uncertain", "Deleted role check", "Deleted role", 400, "У пользователя только одна роль - неопределенная", "Удаление роли у пользователя");
			}
			else
			{
				userSub.SubscribeRoles.Add(new SubscribeRoleDbModel
				{
					RoleId = uncertainRole.Id,
					UserServerId = userSub.Id
				});
				_hitsContext.UserServer.Update(userSub);
				await _hitsContext.SaveChangesAsync();

				var newUserRole = new NewUserRoleResponseDTO
				{
					ServerId = serverId,
					UserId = userId,
					RoleId = uncertainRole.Id,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(newUserRole, alertedUsers, "Role added to user");
				}
			}
		}

		userSub.SubscribeRoles.Remove(deletedRole);
		_hitsContext.UserServer.Update(userSub);
		await _hitsContext.SaveChangesAsync();

		var removedChannels = await _hitsContext.ChannelCanSee
			.Where(ccs => ccs.RoleId == deletedRole.RoleId)
			.Select(ccs => ccs.ChannelId)
			.ToListAsync();
		foreach (var channelId in removedChannels)
		{
			bool stillHasAccess = await _hitsContext.ChannelCanSee
				.AnyAsync(ccs => removedChannels.Contains(ccs.ChannelId)
								 && userSub.SubscribeRoles.Select(sr => sr.RoleId).Contains(ccs.RoleId));

			if (!stillHasAccess)
			{
				var lastRead = await _hitsContext.LastReadChannelMessage
					.FirstOrDefaultAsync(lr => lr.UserId == user.Id && lr.TextChannelId == channelId);

				if (lastRead != null)
					_hitsContext.LastReadChannelMessage.Remove(lastRead);
			}
		}
		await _hitsContext.SaveChangesAsync();

		var oldUserRole = new NewUserRoleResponseDTO
		{
			ServerId = serverId,
			UserId = userId,
			RoleId = deletedRole.RoleId,
		};
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(oldUserRole, alertedUsers, "Role removed from user");
		}
	}

	public async Task<ServerInfoDTO> GetServerInfoAsync(string token, Guid serverId)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var server = await GetServerFullModelAsync(serverId);

		var sub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);
		if (sub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Check user sub", "User sub", 404, "Пользователь не является подписчиком сервера", "Получение информации о сервере");
		}

		var userRoleIds = sub.SubscribeRoles.Select(sr => sr.RoleId).ToHashSet();
		var friendsIds = await _hitsContext.Friendship
			.Where(f => f.UserIdFrom == user.Id || f.UserIdTo == user.Id)
			.Select(f => f.UserIdFrom == user.Id ? f.UserIdTo : f.UserIdFrom)
			.Distinct()
			.ToListAsync();
		var nonNotifiableChannelsList = await _hitsContext.NonNotifiableChannel
			.Include(nnc => nnc.TextChannel)
			.Where(nnc =>
				nnc.UserServerId == sub.Id
				&& nnc.TextChannel.ServerId == server.Id
			)
			.Select(nnc => nnc.TextChannelId)
			.ToListAsync();
		var lastReads = await _hitsContext.LastReadChannelMessage
			.Include(lr => lr.TextChannel)
			.Where(lr => lr.UserId == user.Id && lr.TextChannel.ServerId == server.Id)
			.ToListAsync();
		var lastReadsDict = lastReads.ToDictionary(lr => lr.TextChannelId, lr => lr.LastReadedMessageId);

		var voiceChannelResponses = await _hitsContext.VoiceChannel
			.Include(vc => vc.Users)
			.Include(vc => vc.ChannelCanSee)
			.Include(vc => vc.ChannelCanJoin)
			.Where(vc => vc.ServerId == server.Id
				&& vc.ChannelCanSee.Any(ccs => userRoleIds.Contains(ccs.RoleId)) && EF.Property<string>(vc, "ChannelType") == "Voice")
			.Select(vc => new VoiceChannelResponseDTO
			{
				ChannelName = vc.Name,
				ChannelId = vc.Id,
				CanJoin = vc.ChannelCanJoin.Any(ccj => userRoleIds.Contains(ccj.RoleId)),
				MaxCount = vc.MaxCount,
				Users = vc.Users.Select(u => new VoiceChannelUserDTO
				{
					UserId = u.UserId,
					MuteStatus = u.MutedOther == true ? MuteStatusEnum.Muted : (u.MutedHimself == true ? MuteStatusEnum.SelfMuted : MuteStatusEnum.NotMuted),
					IsStream = u.IsStream
				})
				.ToList()
			})
			.ToListAsync();

		var pairVoiceChannelResponses = server.ServerType == ServerTypeEnum.Teacher ? await _hitsContext.PairVoiceChannel
			.Include(vc => vc.Users)
			.Include(vc => vc.ChannelCanSee)
			.Include(vc => vc.ChannelCanJoin)
			.Where(vc => vc.ServerId == server.Id
				&& vc.ChannelCanSee.Any(ccs => userRoleIds.Contains(ccs.RoleId)))
			.Select(vc => new VoiceChannelResponseDTO
			{
				ChannelName = vc.Name,
				ChannelId = vc.Id,
				CanJoin = vc.ChannelCanJoin.Any(ccj => userRoleIds.Contains(ccj.RoleId)),
				MaxCount = vc.MaxCount,
				Users = vc.Users.Select(u => new VoiceChannelUserDTO
				{
					UserId = u.UserId,
					MuteStatus = u.MutedOther == true ? MuteStatusEnum.Muted : (u.MutedHimself == true ? MuteStatusEnum.SelfMuted : MuteStatusEnum.NotMuted),
					IsStream = u.IsStream
				})
				.ToList()
			})
			.ToListAsync()
			: new List<VoiceChannelResponseDTO>();

		var textChannels = await _hitsContext.TextChannel
			.Include(t => t.ChannelCanSee)
			.Include(t => t.ChannelCanWrite)
				.ThenInclude(ccw => ccw.Role)
			.Include(t => t.ChannelCanWriteSub)
			.Include(t => t.Messages)
			.Where(t => t.ServerId == server.Id && t.ChannelCanSee.Any(ccs => userRoleIds.Contains(ccs.RoleId)) && EF.Property<string>(t, "ChannelType") == "Text" && t.DeleteTime == null)
			.ToListAsync();

		var textChannelResponses = textChannels
			.Select(t =>
			{
				var lastReadId = lastReadsDict.TryGetValue(t.Id, out var lr) ? lr : 0;
				var messages = t.Messages.Where(m => m.Id > lastReadId && m.DeleteTime == null);

				return new TextChannelResponseDTO
				{
					ChannelName = t.Name,
					ChannelId = t.Id,
					CanWrite = t.ChannelCanWrite.Any(ccw => userRoleIds.Contains(ccw.RoleId)),
					CanWriteSub = t.ChannelCanWriteSub.Any(ccws => userRoleIds.Contains(ccws.RoleId)),
					IsNotifiable = nonNotifiableChannelsList.Contains(t.Id),
					NonReadedCount = messages.Count(),
					NonReadedTaggedCount = messages.Count(m =>
						m.TaggedUsers.Contains(user.Id) ||
						m.TaggedRoles.Any(rid => userRoleIds.Contains(rid))
					),
					LastReadedMessageId = lastReadId,
					RolesCanWrite = t.ChannelCanWrite.Select(ccw => new UserServerRoles
						{
							RoleId = ccw.RoleId,
							RoleName = ccw.Role.Name,
							RoleType = ccw.Role.Role
						})
						.ToList()
				};
			})
			.ToList();


		var notificationChannels = await _hitsContext.NotificationChannel
			.Include(n => n.ChannelCanSee)
			.Include(n => n.ChannelCanWrite)
			.Include(n => n.ChannelNotificated)
			.Include(n => n.Messages)
			.Where(n => n.ServerId == server.Id && n.ChannelCanSee.Any(ccs => userRoleIds.Contains(ccs.RoleId)) && n.DeleteTime == null)
			.ToListAsync();

		var notificationChannelResponses = notificationChannels
			.Select(n =>
			{
				var lastReadId = lastReadsDict.TryGetValue(n.Id, out var lr) ? lr : 0;
				var messages = n.Messages.Where(m => m.Id > lastReadId && m.DeleteTime == null);

				return new NotificationChannelResponseDTO
				{
					ChannelName = n.Name,
					ChannelId = n.Id,
					CanWrite = n.ChannelCanWrite.Any(ccw => userRoleIds.Contains(ccw.RoleId)),
					IsNotificated = n.ChannelNotificated.Any(cn => userRoleIds.Contains(cn.RoleId)),
					IsNotifiable = nonNotifiableChannelsList.Contains(n.Id),
					NonReadedCount = messages.Count(),
					NonReadedTaggedCount = messages.Count(m =>
						m.TaggedUsers.Contains(user.Id) ||
						m.TaggedRoles.Any(rid => userRoleIds.Contains(rid))
					),
					LastReadedMessageId = lastReadId
				};
			})
			.ToList();

		var serverUsers = await _hitsContext.UserServer
			.Include(us => us.User)
				.ThenInclude(u => u.IconFile)
			.Include(us => us.User)
				.ThenInclude(u => u.SystemRoles)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.Where(us => us.ServerId == serverId && us.IsBanned == false)
			.Select(us => new ServerUserDTO
			{
				ServerId = us.ServerId,
				UserId = us.UserId,
				UserName = us.UserServerName,
				UserTag = us.User.AccountTag,
				Icon = us.User.IconFile == null ? null : new FileMetaResponseDTO
				{
					FileId = us.User.IconFile.Id,
					FileName = us.User.IconFile.Name,
					FileType = us.User.IconFile.Type,
					FileSize = us.User.IconFile.Size,
					Deleted = us.User.IconFile.Deleted
				},
				Roles = us.SubscribeRoles
					.Select(sr => new UserServerRoles
					{
						RoleId = sr.Role.Id,
						RoleName = sr.Role.Name,
						RoleType = sr.Role.Role
					})
					.ToList(),
				Notifiable = us.User.Notifiable,
				FriendshipApplication = us.User.FriendshipApplication,
				NonFriendMessage = us.User.NonFriendMessage,
				isFriend = friendsIds.Contains(us.User.Id),
				SystemRoles = us.User.SystemRoles
					.Select(sr => new SystemRoleShortItemDTO
					{
						Id = null,
						Name = sr.Name,
						Type = sr.Type
					})
					.ToList()
			})
			.ToListAsync();

		var info = new ServerInfoDTO
		{
			ServerId = serverId,
			ServerName = server.Name,
			ServerType = server.ServerType,
			Icon = null,
			IsClosed = server.IsClosed,
			Roles = await _hitsContext.Role
				.Where(r => r.ServerId == server.Id)
				.Select(r => new RolesItemDTO
				{
					Id = r.Id,
					Name = r.Name,
					ServerId = r.ServerId,
					Tag = r.Tag,
					Color = r.Color,
					Type = r.Role
				})
				.ToListAsync(),
			UserRoles = sub.SubscribeRoles
				.Select(sr => new UserServerRoles
				{
					RoleId = sr.Role.Id,
					RoleName = sr.Role.Name,
					RoleType = sr.Role.Role
				})
				.ToList(),
			IsCreator = sub.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator),
			Permissions = new SettingsDTO
			{
				CanChangeRole = sub.SubscribeRoles.Any(sr => sr.Role.ServerCanChangeRole),
				CanWorkChannels = sub.SubscribeRoles.Any(sr => sr.Role.ServerCanWorkChannels),
				CanDeleteUsers = sub.SubscribeRoles.Any(sr => sr.Role.ServerCanDeleteUsers),
				CanMuteOther = sub.SubscribeRoles.Any(sr => sr.Role.ServerCanMuteOther),
				CanDeleteOthersMessages = sub.SubscribeRoles.Any(sr => sr.Role.ServerCanDeleteOthersMessages),
				CanIgnoreMaxCount = sub.SubscribeRoles.Any(sr => sr.Role.ServerCanIgnoreMaxCount),
				CanCreateRoles = sub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateRoles),
				CanCreateLessons = sub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateLessons),
				CanCheckAttendance = sub.SubscribeRoles.Any(sr => sr.Role.ServerCanCheckAttendance),
				CanUseInvitations = sub.SubscribeRoles.Any(sr => sr.Role.ServerCanUseInvitations)
			},
			IsNotifiable = sub.NonNotifiable,
			Users = serverUsers,
			Channels = new ChannelListDTO
			{
				TextChannels = textChannelResponses,
				NotificationChannels = notificationChannelResponses,
				VoiceChannels = voiceChannelResponses,
				PairVoiceChannels = pairVoiceChannelResponses
			}
		};

		if (server.IconFileId != null)
		{
			var serverIcon = await GetImageAsync((Guid)server.IconFileId);
			info.Icon = serverIcon;
		}

		return info;
	}

	public async Task DeleteUserFromServerAsync(string token, Guid serverId, Guid userId, string? banReason)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Check owner", "Owner", 404, "Пользователь не найден", "Удаление пользователя с сервера");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanDeleteUsers) == false)
		{
			throw new CustomException("Owner does not have rights to delete users", "Check user rights to delete users", "Owner", 403, "Пользователь не имеет права удалять пользователей с сервера", "Удаление пользователя с сервера");
		}

		var user = await _authorizationService.GetUserAsync(userId);
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Check user", "User", 404, "Удаляемый пользователь не найден", "Удаление пользователя с сервера");
		}

		if (userId == owner.Id)
		{
			throw new CustomException("User cant delete himself", "Delete user from server", "User", 400, "Пользователь не может удалить сам себя", "Удаление пользователя с сервера");
		}
		if (ownerSub.SubscribeRoles.Min(sr => sr.Role.Role) > userSub.SubscribeRoles.Min(sr => sr.Role.Role))
		{
			throw new CustomException("Owner lower in ierarchy than deleted user", "Delete user from server", "Changed user role", 401, "Пользователь ниже по иерархии чем удаляемый пользователь", "Удаление пользователя с сервера");
		}

		var lastMessage = await _hitsContext.LastReadChannelMessage.Include(lr => lr.TextChannel).Where(lr => lr.TextChannel.ServerId == server.Id && lr.UserId == userSub.UserId).ToListAsync();
		_hitsContext.LastReadChannelMessage.RemoveRange(lastMessage);

		var nonNitifiables = await _hitsContext.NonNotifiableChannel.Where(nnc => nnc.UserServerId == userSub.Id).ToListAsync();
		_hitsContext.NonNotifiableChannel.RemoveRange(nonNitifiables);

		userSub.IsBanned = true;
		userSub.BanReason = banReason;
		userSub.BanTime = DateTime.UtcNow;
		_hitsContext.UserServer.Update(userSub);
		var userVoiceChannel = await _hitsContext.UserVoiceChannel.Include(uvc => uvc.VoiceChannel).FirstOrDefaultAsync(uvc => uvc.UserId == userId && uvc.VoiceChannel.ServerId == serverId);
		var newRemovedUserResponse = new RemovedUserDTO
		{
			ServerId = serverId,
			IsNeedRemoveFromVC = userVoiceChannel != null
		};
		await _hitsContext.SaveChangesAsync();

		await _webSocketManager.BroadcastMessageAsync(newRemovedUserResponse, new List<Guid> { userId }, "You removed from server");

		var newUnsubscriberResponse = new UnsubscribeResponseDTO
		{
			ServerId = serverId,
			UserId = userId,
		};
		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, alertedUsers, "User unsubscribe");
		}

		await _hitsContext.Notifications.AddAsync(new NotificationDbModel
		{
			UserId = userId,
			Text = $"Вы были забанены на сервере: {server.Name}",
			CreatedAt = DateTime.UtcNow,
			IsReaded = false
		});
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeServerNameAsync(Guid serverId, string token, string name)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner not subscriber of this server", "Check subscription is exist", "User", 401, "Владелец не является участником этого сервера", "Изменение названия сервера");
		}
		if (!ownerSub.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator))
		{
			throw new CustomException("User is not creator of this server", "Check subscription roles", "User", 401, "Пользователь - не создатель сервера", "Изменение названия сервера");
		}


		server.Name = name;
		_hitsContext.Server.Update(server);
		await _hitsContext.SaveChangesAsync();

        var changeServerName = new ChangeNameDTO
        {
            Id = serverId,
            Name = name
        };
		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changeServerName, alertedUsers, "New server name");
		}
	}

	public async Task ChangeUserNameAsync(Guid serverId, string token, string name)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
        {
			throw new CustomException("User not subscriber of this server", "Change user name", "User", 400, "Пользователь не является подписчикаом", "Изменение имени на сервере");
		}
		ownerSub.UserServerName = name;
		_hitsContext.UserServer.Update(ownerSub);
		await _hitsContext.SaveChangesAsync();


		var changeServerName = new ChangeNameOnServerDTO
		{
			ServerId = serverId,
            UserId = owner.Id,
			Name = name
		};
		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changeServerName, alertedUsers, "New users name on server");
		}
	}

	public async Task ChangeNonNotifiableServerAsync(string token, Guid serverId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("User not subscriber of this server", "Change NonNotifiable Server", "User", 400, "Пользователь не является подписчикаом", "Изменение уведомляемости сервера");
		}
		ownerSub.NonNotifiable = !ownerSub.NonNotifiable;
		_hitsContext.UserServer.Update(ownerSub);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task<BanListDTO> GetBannedListAsync(string token, Guid serverId, int page, int size)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Check owner", "Owner", 404, "Владелец не найден", "Получение списка забаненных");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanDeleteUsers) == false)
		{
			throw new CustomException("Owner does not have rights to delete users", "Check user rights to delete users", "Owner", 403, "Владелец не имеет права удалять пользователей", "Получение списка забаненных");
		}
		var bannedCount = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id && us.IsBanned == true).CountAsync();
		if (page < 1 || size < 1)
		{
			throw new CustomException($"Pagination error", "Get banned list", "pagination", 400, $"Проблема с пагинацией", "Получение списка забаненных");
		}
		if (bannedCount == 0)
		{
			return (new BanListDTO
			{
				BannedList = new List<ServerBannedUserDTO>(),
				Page = page,
				Size = size,
				Total = 0
			});
		}
		if (((page - 1) * size) + 1 > bannedCount)
		{
			throw new CustomException($"Pagination error", "Get banned list", "pagination", 400, $"Проблема с пагинацией", "Получение списка забаненных");
		}
		var bannedUsers = new BanListDTO
		{
			BannedList = await _hitsContext.UserServer
				.OrderBy(m => m.BanTime)
				.Skip((page - 1) * size)
				.Take(size)
				.Include(us => us.User)
				.Where(us => 
					us.ServerId == server.Id
					&& us.IsBanned == true)
				.Select(us => new ServerBannedUserDTO
				{
					UserId = us.UserId,
					UserName = us.UserServerName,
					UserTag = us.User.AccountTag,
					BanReason = us.BanReason,
					BanTime = (DateTime)us.BanTime
				})
				.ToListAsync(),
			Page = page,
			Size = size,
			Total = bannedCount
		};

		return bannedUsers;
	}

	public async Task UnBanUser(string token, Guid serverId, Guid bannedId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Check owner", "Owner", 404, "Владелец не найден", "Разбан пользователя");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanDeleteUsers) == false)
		{
			throw new CustomException("Owner does not have rights to delete users", "Check user rights to delete users", "Owner", 403, "Владелец не имеет права удалять пользователей", "Разбан пользователя");
		}

		var banned = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.ServerId == serverId && us.UserId == bannedId && us.IsBanned == true);
		if (banned == null)
		{
			throw new CustomException("Banned user not found", "Unban user", "User", 404, "Забаненный пользователь не найден", "Разбан пользователя");
		}

		_hitsContext.UserServer.Remove(banned);
		await _hitsContext.SaveChangesAsync();

		var lastReaded = await _hitsContext.LastReadChannelMessage.Include(lrcm => lrcm.TextChannel).Where(lrcm => lrcm.UserId == banned.UserId && lrcm.TextChannel.ServerId == banned.ServerId).ToListAsync();
		_hitsContext.LastReadChannelMessage.RemoveRange(lastReaded);
		await _hitsContext.SaveChangesAsync();

		await _hitsContext.Notifications.AddAsync(new NotificationDbModel
		{
			UserId = banned.UserId,
			Text = $"Вы были разбанены на сервере: {server.Name}",
			CreatedAt = DateTime.UtcNow,
			IsReaded = false,
			ServerId = server.Id
		});
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeServerIconAsync(string token, Guid serverId, IFormFile iconFile)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner not subscriber of this server", "Check subscription is exist", "User", 401, "Владелец не является участником этого сервера", "Изменение иконки сервера");
		}
		if (!ownerSub.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator))
		{
			throw new CustomException("User is not creator of this server", "Check subscription roles", "User", 401, "Пользователь - не создатель сервера", "Изменение иконки сервера");
		}

		if (iconFile.Length > 10 * 1024 * 1024)
		{
			throw new CustomException("Icon too large", "Сhange server icon", "Icon", 400, "Файл слишком большой (макс. 10 МБ)", "Изменение иконки сервера");
		}

		if (!iconFile.ContentType.StartsWith("image/"))
		{
			throw new CustomException("Invalid file type", "Сhange server icon", "Icon", 400, "Файл не является изображением!", "Изменение иконки сервера");
		}

		byte[] fileBytes;
		using (var ms = new MemoryStream())
		{
			await iconFile.CopyToAsync(ms);
			fileBytes = ms.ToArray();
		}

		var scanResult = await _clamService.ScanFileAsync(fileBytes);
		if (scanResult.Result != ClamScanResults.Clean)
		{
			throw new CustomException("Virus detected", "Сhange server icon", "Icon", 400, "Обнаружен вирус в файле", "Изменение иконки сервера");
		}

		using var imgStream = new MemoryStream(fileBytes);
		SixLabors.ImageSharp.Image image;
		try
		{
			image = await SixLabors.ImageSharp.Image.LoadAsync(imgStream);
		}
		catch (SixLabors.ImageSharp.UnknownImageFormatException)
		{
			throw new CustomException("Invalid image file", "Сhange server icon", "Icon", 400, "Файл не является валидным изображением!", "Изменение иконки сервера");
		}

		if (image.Width > 650 || image.Height > 650)
		{
			throw new CustomException("Icon too large", "Сhange server icon", "Icon", 400, "Изображение слишком большое (макс. 650x650)", "Изменение иконки сервера");
		}

		var originalFileName = Path.GetFileName(iconFile.FileName);
		var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
		var objectName = $"icons/{safeFileName}";

		await _minioService.UploadFileAsync(objectName, fileBytes, iconFile.ContentType);
		if (server.IconFileId != null)
		{
			var oldIcon = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == server.IconFileId);
			if (oldIcon != null)
			{
				try
				{
					await _minioService.DeleteFileAsync(oldIcon.Path);
				}
				catch
				{
				}
				_hitsContext.File.Remove(oldIcon);
				await _hitsContext.SaveChangesAsync();
			}
		}

		var file = new FileDbModel
		{
			Id = Guid.NewGuid(),
			Path = objectName,
			Name = originalFileName,
			Type = iconFile.ContentType,
			Size = iconFile.Length,
			Creator = owner.Id,
			IsApproved = true,
			CreatedAt = DateTime.UtcNow,
			Deleted = false,
			ServerId = server.Id
		};

		_hitsContext.File.Add(file);
		await _hitsContext.SaveChangesAsync();

		server.IconFileId = file.Id;
		_hitsContext.Server.Update(server);
		await _hitsContext.SaveChangesAsync();

		string base64Icon = Convert.ToBase64String(fileBytes);
		var changeIconDto = new ServerIconResponseDTO
		{
			ServerId = server.Id,
			Icon = new FileMetaResponseDTO
			{
				FileId = file.Id,
				FileName = file.Name,
				FileType = file.Type,
				FileSize = file.Size,
				Deleted = file.Deleted
			}
		};

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Any())
		{
			await _webSocketManager.BroadcastMessageAsync(changeIconDto, alertedUsers, "New icon on server");
		}
	}

	public async Task DeleteServerIconAsync(string token, Guid serverId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner not subscriber of this server", "DeleteServerIconAsync", "User", 401, "Владелец не является участником этого сервера", "Удаление иконки сервера");
		}
		if (!ownerSub.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator))
		{
			throw new CustomException("User is not creator of this server", "DeleteServerIconAsync", "User", 401, "Пользователь - не создатель сервера", "Удаление иконки сервера");
		}

		if (server.IconFileId == null)
		{
			throw new CustomException("This server has no icon", "DeleteServerIconAsync", "IconFileId", 404, "У этого сервера нет иконки", "Удаление иконки сервера");
		}

		var oldIcon = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == server.IconFileId);
		if (oldIcon == null)
		{
			throw new CustomException("Icon not found", "DeleteServerIconAsync", "IconFileId", 404, "Файл не найден", "Удаление иконки сервера");
		}

		try
		{
			await _minioService.DeleteFileAsync(oldIcon.Path);
		}
		catch
		{
		}

		server.IconFileId = null;
		_hitsContext.Server.Update(server);
		_hitsContext.File.Remove(oldIcon);
		await _hitsContext.SaveChangesAsync();

		var serverIdResponse = new IdRequestDTO { Id = server.Id };

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Any())
		{
			await _webSocketManager.BroadcastMessageAsync(serverIdResponse, alertedUsers, "Icon removed from server");
		}
	}

	public async Task ChangeServerClosedAsync(string token, Guid serverId, bool isClosed, bool? isApproved)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner not subscriber of this server", "Check subscription is exist", "User", 401, "Владелец не является участником этого сервера", "Изменение закрытости сервера");
		}
		if (!ownerSub.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator))
		{
			throw new CustomException("User is not creator of this server", "Check subscription roles", "User", 401, "Пользователь - не создатель сервера", "Изменение закрытости сервера");
		}

		if (server.IsClosed == isClosed)
		{
			throw new CustomException($"Server isClosed is already {isClosed}", "Сhange server isClosed", "isClosed", 400, $"Закрытость сервера уже {isClosed}", "Изменение закрытости сервера");
		}

		server.IsClosed = isClosed;
		_hitsContext.Server.Update(server);
		await _hitsContext.SaveChangesAsync();

		if (isClosed == false)
		{
			if (isApproved == null)
			{
				throw new CustomException($"isApproved is required", "Сhange server isClosed", "isApproved", 400, $"isApproved необходимо", "Изменение закрытости сервера");
			}

			var applications = await _hitsContext.ServerApplications.Where(sa => sa.ServerId == server.Id).ToListAsync();
			if (applications != null && applications.Count() > 0)
			{
				if (isApproved == true)
				{
					var uncertainRole = server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain);
					if (uncertainRole == null)
					{
						throw new CustomException("Uncertain role not found", "Check uncertain role is exist", "uncertainRole", 400, "Неопределенная роль не нйдена", "Подписка");
					}
					foreach (var application in applications)
					{
						var user = await _authorizationService.GetUserAsync(application.UserId);
						var newSub = new UserServerDbModel
						{
							Id = Guid.NewGuid(),
							UserId = user.Id,
							ServerId = server.Id,
							UserServerName = user.AccountName,
							IsBanned = false,
							NonNotifiable = false,
							SubscribeRoles = new List<SubscribeRoleDbModel>()
						};
						newSub.SubscribeRoles.Add(new SubscribeRoleDbModel
						{
							UserServerId = newSub.Id,
							RoleId = uncertainRole.Id
						});

						var channelsCanRead = await _hitsContext.ChannelCanSee.Include(ccs => ccs.Channel).Where(ccs => (ccs.Channel is TextChannelDbModel || ccs.Channel is NotificationChannelDbModel || ccs.Channel is SubChannelDbModel) && ccs.Channel.ServerId == server.Id && ccs.RoleId == uncertainRole.Id).Select(ccs => ccs.ChannelId).ToListAsync();
						var lastReadedList = new List<LastReadChannelMessageDbModel>();
						if (channelsCanRead != null)
						{
							foreach (var channel in channelsCanRead)
							{
								lastReadedList.Add(new LastReadChannelMessageDbModel
								{
									UserId = user.Id,
									TextChannelId = channel,
									LastReadedMessageId = (await _hitsContext.ChannelMessage.Select(m => (long?)m.Id).MaxAsync() ?? 0)
								});
							}
						}

						await _hitsContext.UserServer.AddAsync(newSub);
						await _hitsContext.LastReadChannelMessage.AddRangeAsync(lastReadedList);
						await _hitsContext.SaveChangesAsync();
						_hitsContext.ServerApplications.Remove(application);
						await _hitsContext.SaveChangesAsync();


						var newSubscriberResponse = new ServerUserDTO
						{
							ServerId = serverId,
							UserId = user.Id,
							UserName = user.AccountName,
							UserTag = user.AccountTag,
							Icon = null,
							Roles = new List<UserServerRoles>{
								new UserServerRoles
								{
									RoleId = uncertainRole.Id,
									RoleName = uncertainRole.Name,
									RoleType = uncertainRole.Role
								}
							},
							Notifiable = user.Notifiable,
							FriendshipApplication = user.FriendshipApplication,
							NonFriendMessage = user.NonFriendMessage,
							isFriend = false,
							SystemRoles = user.SystemRoles
								.Select(sr => new SystemRoleShortItemDTO
								{
									Name = sr.Name,
									Type = sr.Type
								})
								.ToList()
						};
						if (user != null && user.IconFileId != null)
						{
							var userIcon = await GetImageAsync((Guid)user.IconFileId);
							newSubscriberResponse.Icon = userIcon;
						}
						var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
						alertedUsers = alertedUsers.Where(a => a != user.Id).ToList();
						if (alertedUsers != null && alertedUsers.Count() > 0)
						{
							await _webSocketManager.BroadcastMessageAsync(newSubscriberResponse, alertedUsers, "New user on server");
						}

						await _hitsContext.Notifications.AddAsync(new NotificationDbModel
						{
							UserId = user.Id,
							Text = $"Вашу заявку приняли на сервере: {server.Name}",
							CreatedAt = DateTime.UtcNow,
							IsReaded = false,
							ServerId = server.Id
						});
						await _hitsContext.SaveChangesAsync();
					}
				}
				else
				{
					_hitsContext.ServerApplications.RemoveRange(applications);
					await _hitsContext.SaveChangesAsync();

					foreach (var app in applications)
					{
						await _hitsContext.Notifications.AddAsync(new NotificationDbModel
						{
							UserId = app.UserId,
							Text = $"Вашу заявку отклонили на сервере: {server.Name}",
							CreatedAt = DateTime.UtcNow,
							IsReaded = false
						});
						await _hitsContext.SaveChangesAsync();
					}
				}
			}
		}
	}

	public async Task ApproveApplicationAsync(string token, Guid applicationId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var application = await _hitsContext.ServerApplications.FirstOrDefaultAsync(sa => sa.Id == applicationId);
		if (application == null)
		{
			throw new CustomException($"Application not found", "Approve application", "applicationId", 404, $"Заявка не найдена", "Подтверждение заявки");
		}
		var user = await _authorizationService.GetUserAsync(application.UserId);
		var server = await CheckServerExistAsync(application.ServerId, false);
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Check owner", "Owner", 404, "Пользователь не найден", "Удаление пользователя с сервера");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanDeleteUsers) == false)
		{
			throw new CustomException("Owner does not have rights to delete users", "Check user rights to delete users", "Owner", 403, "Пользователь не имеет права удалять пользователей с сервера", "Удаление пользователя с сервера");
		}
		var uncertainRole = server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain);
		if (uncertainRole == null)
		{
			throw new CustomException("Uncertain role not found", "Check uncertain role is exist", "uncertainRole", 400, "Неопределенная роль не нйдена", "Подписка");
		}
		var newSub = new UserServerDbModel
		{
			Id = Guid.NewGuid(),
			UserId = user.Id,
			ServerId = server.Id,
			UserServerName = user.AccountName,
			IsBanned = false,
			NonNotifiable = false,
			SubscribeRoles = new List<SubscribeRoleDbModel>()
		};
		newSub.SubscribeRoles.Add(new SubscribeRoleDbModel
		{
			UserServerId = newSub.Id,
			RoleId = uncertainRole.Id
		});

		var channelsCanRead = await _hitsContext.ChannelCanSee.Include(ccs => ccs.Channel).Where(ccs => (ccs.Channel is TextChannelDbModel || ccs.Channel is NotificationChannelDbModel || ccs.Channel is SubChannelDbModel) && ccs.Channel.ServerId == server.Id && ccs.RoleId == uncertainRole.Id).Select(ccs => ccs.ChannelId).ToListAsync();
		var lastReadedList = new List<LastReadChannelMessageDbModel>();
		if (channelsCanRead != null)
		{
			foreach (var channel in channelsCanRead)
			{
				lastReadedList.Add(new LastReadChannelMessageDbModel
				{
					UserId = user.Id,
					TextChannelId = channel,
					LastReadedMessageId = (await _hitsContext.ChannelMessage.Select(m => (long?)m.Id).MaxAsync() ?? 0)
				});
			}
		}

		await _hitsContext.UserServer.AddAsync(newSub);
		await _hitsContext.LastReadChannelMessage.AddRangeAsync(lastReadedList);
		_hitsContext.ServerApplications.Remove(application);
		await _hitsContext.SaveChangesAsync();

		var newSubscriberResponse = new ServerUserDTO
		{
			ServerId = server.Id,
			UserId = user.Id,
			UserName = user.AccountName,
			UserTag = user.AccountTag,
			Icon = null,
			Roles = new List<UserServerRoles>{
				new UserServerRoles
				{
					RoleId = uncertainRole.Id,
					RoleName = uncertainRole.Name,
					RoleType = uncertainRole.Role
				}
			},
			Notifiable = user.Notifiable,
			FriendshipApplication = user.FriendshipApplication,
			NonFriendMessage = user.NonFriendMessage,
			isFriend = false,
			SystemRoles = user.SystemRoles
				.Select(sr => new SystemRoleShortItemDTO
				{
					Name = sr.Name,
					Type = sr.Type
				})
				.ToList()
		};
		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		alertedUsers = alertedUsers.Where(a => a != user.Id).ToList();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newSubscriberResponse, alertedUsers, "New user on server");
		}

		await _hitsContext.Notifications.AddAsync(new NotificationDbModel
		{
			UserId = user.Id,
			Text = $"Вашу заявку приняли на сервере: {server.Name}",
			CreatedAt = DateTime.UtcNow,
			IsReaded = false,
			ServerId = server.Id
		});
		await _hitsContext.SaveChangesAsync();
	}

	public async Task RemoveApplicationServerAsync(string token, Guid applicationId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var application = await _hitsContext.ServerApplications.FirstOrDefaultAsync(sa => sa.Id == applicationId);
		if (application == null)
		{
			throw new CustomException($"Application not found", "Remove application server", "applicationId", 404, $"Заявка не найдена", "Отклонение заявки от сервера");
		}
		var server = await CheckServerExistAsync(application.ServerId, false);
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Check owner", "Owner", 404, "Пользователь не найден", "Удаление пользователя с сервера");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanDeleteUsers) == false)
		{
			throw new CustomException("Owner does not have rights to delete users", "Check user rights to delete users", "Owner", 403, "Пользователь не имеет права удалять пользователей с сервера", "Удаление пользователя с сервера");
		}

		_hitsContext.ServerApplications.Remove(application);
		await _hitsContext.SaveChangesAsync();

		await _hitsContext.Notifications.AddAsync(new NotificationDbModel
		{
			UserId = application.UserId,
			Text = $"Вашу заявку отклонили на сервере: {server.Name}",
			CreatedAt = DateTime.UtcNow,
			IsReaded = false
		});
		await _hitsContext.SaveChangesAsync();
	}

	public async Task RemoveApplicationUserAsync(string token, Guid applicationId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var application = await _hitsContext.ServerApplications.FirstOrDefaultAsync(sa => sa.Id == applicationId && sa.UserId == owner.Id);
		if (application == null)
		{
			throw new CustomException($"Application not found", "Remove application user", "applicationId", 404, $"Заявка не найдена", "Отклонение заявки пользователем");
		}

		_hitsContext.ServerApplications.Remove(application);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task<ServerApplicationsListResponseDTO> GetServerApplicationsAsync(string token, Guid serverId, int page, int size)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Check owner", "Owner", 404, "Пользователь не найден", "Удаление пользователя с сервера");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanDeleteUsers) == false)
		{
			throw new CustomException("Owner does not have rights to delete users", "Check user rights to delete users", "Owner", 403, "Пользователь не имеет права удалять пользователей с сервера", "Удаление пользователя с сервера");
		}
		var applicationsCount = await _hitsContext.ServerApplications.Where(sa => sa.ServerId == server.Id).CountAsync();
		if (page < 1 || size < 1)
		{
			throw new CustomException($"Pagination error", "Get server applications", "pagination", 400, $"Проблема с пагинацией", "Получение заявок сервера");
		}
		if (applicationsCount == 0)
		{
			return (new ServerApplicationsListResponseDTO
			{
				Applications = new List<ServerApplicationResponseDTO>(),
				Page = page,
				Size = size,
				Total = 0
			});
		}
		if (((page - 1) * size) + 1 > applicationsCount)
		{
			throw new CustomException($"Pagination error", "Get server applications", "pagination", 400, $"Проблема с пагинацией", "Получение заявок сервера");
		}
		var applications = await _hitsContext.ServerApplications
			.Where(sa => sa.ServerId == server.Id)
			.OrderBy(m => m.CreatedAt)
			.Skip((page - 1) * size)
			.Take(size)
			.Include(sa => sa.User)
				.ThenInclude(u => u.SystemRoles)
			.Select(sa => new ServerApplicationResponseDTO
			{
				ApplicationId = sa.Id,
				ServerId = sa.ServerId,
				User = new ProfileDTO
				{
					Id = sa.User.Id,
					Name = sa.User.AccountName,
					Tag = sa.User.AccountTag,
					AccontCreateDate = DateOnly.FromDateTime(sa.User.AccountCreateDate),
					Notifiable = sa.User.Notifiable,
					FriendshipApplication = sa.User.FriendshipApplication,
					NonFriendMessage = sa.User.NonFriendMessage,
					Icon = null,
					SystemRoles = sa.User.SystemRoles
						.Select(sr => new SystemRoleShortItemDTO
						{
							Name = sr.Name,
							Type = sr.Type
						})
						.ToList()
				},
				CreatedAt = sa.CreatedAt
			})
			.ToListAsync();

		var applicationsList = new ServerApplicationsListResponseDTO
		{
			Applications = applications,
			Page = page,
			Size = size,
			Total = applicationsCount
		};
		return applicationsList;
	}

	public async Task<UserApplicationsListResponseDTO> GetUserApplicationsAsync(string token, int page, int size)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var applicationsCount = await _hitsContext.ServerApplications.Where(sa => sa.UserId == owner.Id).CountAsync();
		if (page < 1 || size < 1 || ((page - 1) * size) + 1 < applicationsCount)
		{
			throw new CustomException($"Pagination error", "Get user applications", "pagination", 400, $"Проблема с пагинацией", "Получение заявок пользователя");
		}
		var applications = await _hitsContext.ServerApplications
			.Where(sa => sa.UserId == owner.Id)
			.OrderBy(sa => sa.CreatedAt)
			.Skip((page - 1) * size)
			.Take(size)
			.Include(sa => sa.Server)
			.Select(sa => new UserApplicationResponseDTO
			{
				ApplicationId = sa.Id,
				ServerId = sa.ServerId,
				ServerName = sa.Server.Name,
				CreatedAt = sa.CreatedAt
			})
			.ToListAsync();

		var applicationsList = new UserApplicationsListResponseDTO
		{
			Applications = applications,
			Page = page,
			Size = size,
			Total = applicationsCount
		};
		return applicationsList;
	}

	public async Task<ServerPresetListResponseDTO> GetServerPresetsAsync(string token, Guid serverId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		if (server.ServerType != ServerTypeEnum.Teacher)
		{
			throw new CustomException("Server isnt teachers", "Get server presets", "Server", 401, "Сервер не является учительсяким", "Получение пресетов сервера");
		}

		var ownerSub = await _hitsContext.UserServer
		.Include(us => us.SubscribeRoles)
			.ThenInclude(sr => sr.Role)
		.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner not subscriber of this server", "Get server presets", "User", 401, "Владелец не является участником этого сервера", "Получение пресетов сервера");
		}
		if (!ownerSub.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator))
		{
			throw new CustomException("User is not creator of this server", "Get server presets", "User", 401, "Пользователь - не создатель сервера", "Получение пресетов сервера");
		}

		var presets = await _hitsContext.Preset
			.Include(p => p.ServerRole)
			.Include(p => p.SystemRole)
			.Where(p => p.ServerRole.ServerId == server.Id)
			.Select(p => new ServerPresetItemDTO
			{
				ServerRoleId = p.ServerRoleId,
				ServerRoleName = p.ServerRole.Name,
				SystemRoleId = p.SystemRoleId,
				SystemRoleName = p.SystemRole.Name,
				SystemRoleType = p.SystemRole.Type
			})
			.ToListAsync();

		return new ServerPresetListResponseDTO { Presets = presets, Total = presets.Count };
	}

	public async Task<SystemRolesFullListNoneChildsDTO> RolesFullListAsync(string token, Guid serverId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		if (server.ServerType != ServerTypeEnum.Teacher)
		{
			throw new CustomException("Server isnt teachers", "Get system roles", "Server", 401, "Сервер не является учительсяким", "Получение системных ролей");
		}

		var ownerSub = await _hitsContext.UserServer
		.Include(us => us.SubscribeRoles)
			.ThenInclude(sr => sr.Role)
		.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner not subscriber of this server", "Get system roles", "User", 401, "Владелец не является участником этого сервера", "Получение системных ролей");
		}
		if (!ownerSub.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator))
		{
			throw new CustomException("User is not creator of this server", "Get system roles", "User", 401, "Пользователь - не создатель сервера", "Получение системных ролей");
		}

		var allRoles = await _hitsContext.SystemRole
			.Include(r => r.ParentRole)
			.Select(r => new SystemRoleNoneListItemDTO
			{
					Id = r.Id,
					Type = r.Type,
					Name = r.Name,
					ParentId = r.ParentRoleId,
					ParentName = r.ParentRole != null ? r.ParentRole.Name : null
				})	
			.ToListAsync();

		var roles = new SystemRolesFullListNoneChildsDTO
		{
			Roles = allRoles
		};

		return roles;
	}

	public async Task<ServerPresetItemDTO> CreatePresetAsync(string token, Guid serverId, Guid serverRoleId, Guid systemRoleId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		if (server.ServerType != ServerTypeEnum.Teacher)
		{
			throw new CustomException("Server isnt teachers", "Create server presets", "Server", 401, "Сервер не является учительсяким", "Создание пресета");
		}

		var serverRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == serverRoleId && r.ServerId == server.Id);
		if (serverRole == null)
		{
			throw new CustomException("Server role not found", "Create server presets", "Server role", 404, "Серверная роль не найдена", "Создание пресета");
		}

		var systemRole = await _hitsContext.SystemRole.FirstOrDefaultAsync(r => r.Id == systemRoleId);
		if (systemRole == null)
		{
			throw new CustomException("System role not found", "Create server presets", "System role", 404, "Системная роль не найдена", "Создание пресета");
		}

		var preset = await _hitsContext.Preset.FirstOrDefaultAsync(p => p.SystemRoleId == systemRole.Id && p.ServerRoleId == serverRole.Id);
		if (preset != null)
		{
			throw new CustomException("Preset already exist", "Create server presets", "Preset", 400, "Такой пресет уже существует", "Создание пресета");
		}

		var newPreset = new ServerPresetDbModel
		{
			SystemRoleId = systemRole.Id,
			ServerRoleId = serverRole.Id
		};

		await _hitsContext.Preset.AddAsync(newPreset);
		await _hitsContext.SaveChangesAsync();

		var users = await _hitsContext.User
			.Include(u => u.SystemRoles)
			.Where(u => u.SystemRoles.Any(sr => sr.Id == systemRoleId))
			.ToListAsync();

		var channelsCanRead = await _hitsContext.ChannelCanSee
			.Include(ccs => ccs.Channel)
			.Where(ccs => (ccs.Channel is TextChannelDbModel || ccs.Channel is NotificationChannelDbModel || ccs.Channel is SubChannelDbModel)
				&& ccs.Channel.ServerId == server.Id
				&& ccs.RoleId == serverRole.Id)
			.Select(ccs => ccs.ChannelId)
			.ToListAsync();

		foreach (var user in users)
		{
			var userSub = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
				.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);

			if (userSub == null || !(userSub.SubscribeRoles.Any(sr => sr.RoleId == serverRole.Id)))
			{
				var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();

				if (userSub == null)
				{
					var newSub = new UserServerDbModel
					{
						Id = Guid.NewGuid(),
						UserId = user.Id,
						ServerId = server.Id,
						UserServerName = user.AccountName,
						IsBanned = false,
						NonNotifiable = false,
						SubscribeRoles = new List<SubscribeRoleDbModel>()
					};
					newSub.SubscribeRoles.Add(new SubscribeRoleDbModel
					{
						UserServerId = newSub.Id,
						RoleId = serverRole.Id
					});

					var lastReadedList = new List<LastReadChannelMessageDbModel>();
					if (channelsCanRead != null)
					{
						foreach (var channel in channelsCanRead)
						{
							lastReadedList.Add(new LastReadChannelMessageDbModel
							{
								UserId = user.Id,
								TextChannelId = channel,
								LastReadedMessageId = (
									await _hitsContext.ChannelMessage
										.Where(cm => cm.TextChannelId == channel)
										.Select(m => (long?)m.Id).MaxAsync() ?? 0
								)
							});
						}
					}

					await _hitsContext.UserServer.AddAsync(newSub);
					await _hitsContext.LastReadChannelMessage.AddRangeAsync(lastReadedList);
					await _hitsContext.SaveChangesAsync();

					var newSubscriberResponse = new ServerUserDTO
					{
						ServerId = server.Id,
						UserId = user.Id,
						UserName = user.AccountName,
						UserTag = user.AccountTag,
						Icon = null,
						Roles = new List<UserServerRoles>{
							new UserServerRoles
							{
								RoleId = serverRole.Id,
								RoleName = serverRole.Name,
								RoleType = serverRole.Role
							}
						},
						Notifiable = user.Notifiable,
						FriendshipApplication = user.FriendshipApplication,
						NonFriendMessage = user.NonFriendMessage,
						isFriend = false,
						SystemRoles = user.SystemRoles
							.Select(sr => new SystemRoleShortItemDTO
							{
								Name = sr.Name,
								Type = sr.Type
							})
							.ToList()
					};
					if (user != null && user.IconFileId != null)
					{
						var userIcon = await GetImageAsync((Guid)user.IconFileId);
						newSubscriberResponse.Icon = userIcon;
					}

					var allFriends = await _hitsContext.Friendship
						.Where(f => f.UserIdFrom == user.Id || f.UserIdTo == user.Id)
						.Select(f => f.UserIdFrom == user.Id ? f.UserIdTo : f.UserIdFrom)
						.Distinct()
						.ToListAsync();
					var friendsSet = new HashSet<Guid>(allFriends);
					if (alertedUsers != null && alertedUsers.Count() > 0)
					{
						foreach (var alertedUser in alertedUsers)
						{
							newSubscriberResponse.isFriend = friendsSet.Contains(alertedUser);
							await _webSocketManager.BroadcastMessageAsync(newSubscriberResponse, new List<Guid> { alertedUser }, "New user on server");
						}
					}
				}
				else
				{
					userSub.SubscribeRoles.Add(new SubscribeRoleDbModel
					{
						UserServerId = userSub.Id,
						RoleId = serverRole.Id
					});

					_hitsContext.UserServer.Update(userSub);
					await _hitsContext.SaveChangesAsync();

					foreach (var channel in channelsCanRead)
					{
						bool alreadyExists = await _hitsContext.LastReadChannelMessage
							.AnyAsync(lr => lr.UserId == user.Id && lr.TextChannelId == channel);

						if (!alreadyExists)
						{
							var lastMessageId = await _hitsContext.ChannelMessage
								.Where(m => m.TextChannelId == channel)
								.OrderByDescending(m => m.Id)
								.Select(m => (long?)m.Id)
								.FirstOrDefaultAsync() ?? 0;

							var lastRead = new LastReadChannelMessageDbModel
							{
								UserId = user.Id,
								TextChannelId = channel,
								LastReadedMessageId = lastMessageId
							};

							await _hitsContext.LastReadChannelMessage.AddAsync(lastRead);
						}
					}
					await _hitsContext.SaveChangesAsync();

					var newUserRole = new NewUserRoleResponseDTO
					{
						ServerId = server.Id,
						UserId = user.Id,
						RoleId = serverRole.Id,
					};
					if (alertedUsers != null && alertedUsers.Count() > 0)
					{
						await _webSocketManager.BroadcastMessageAsync(newUserRole, alertedUsers, "Role added to user");
					}
				}
			}
		}

		var response = new ServerPresetItemDTO
		{
			ServerRoleId = serverRole.Id,
			ServerRoleName = serverRole.Name,
			SystemRoleId = systemRole.Id,
			SystemRoleName = systemRole.Name,
			SystemRoleType = systemRole.Type
		};
		return response;
	}

	public async Task DeletePresetAsync(string token, Guid serverId, Guid serverRoleId, Guid systemRoleId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		if (server.ServerType != ServerTypeEnum.Teacher)
		{
			throw new CustomException("Server isnt teachers", "Delete server presets", "Server", 401, "Сервер не является учительсяким", "Удаление пресета");
		}

		var serverRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == serverRoleId && r.ServerId == server.Id);
		if (serverRole == null)
		{
			throw new CustomException("Server role not found", "Delete server presets", "Server role", 404, "Серверная роль не найдена", "Удаление пресета");
		}

		var systemRole = await _hitsContext.SystemRole.FirstOrDefaultAsync(r => r.Id == systemRoleId);
		if (systemRole == null)
		{
			throw new CustomException("System role not found", "Delete server presets", "System role", 404, "Системная роль не найдена", "Удаление пресета");
		}

		var preset = await _hitsContext.Preset.FirstOrDefaultAsync(p => p.SystemRoleId == systemRole.Id && p.ServerRoleId == serverRole.Id);
		if (preset == null)
		{
			throw new CustomException("Preset not found", "Delete server presets", "Preset", 404, "Пресет не найден", "Удаление пресета");
		}

		var usersSubs = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.Where(us => us.ServerId == server.Id
				&& us.SubscribeRoles.Any(sr => sr.RoleId == serverRole.Id))
			.ToListAsync();

		foreach (var sub in usersSubs)
		{
			var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
			if (sub.SubscribeRoles.Count() == 1)
			{
				var userVoiceChannel = await _hitsContext.UserVoiceChannel
					.Include(us => us.VoiceChannel)
					.FirstOrDefaultAsync(us =>
						us.VoiceChannel.ServerId == server.Id
						&& us.UserId == sub.UserId);
				if (userVoiceChannel != null)
				{
					_hitsContext.UserVoiceChannel.Remove(userVoiceChannel);
				}

				var lastMessage = await _hitsContext.LastReadChannelMessage.Include(lr => lr.TextChannel).Where(lr => lr.UserId == sub.UserId && lr.TextChannel.ServerId == server.Id).ToListAsync();
				_hitsContext.LastReadChannelMessage.RemoveRange(lastMessage);

				var nonNitifiables = await _hitsContext.NonNotifiableChannel.Where(nnc => nnc.UserServerId == sub.Id).ToListAsync();
				_hitsContext.NonNotifiableChannel.RemoveRange(nonNitifiables);

				_hitsContext.UserServer.Remove(sub);
				await _hitsContext.SaveChangesAsync();

				var newUnsubscriberResponse = new UnsubscribeResponseDTO
				{
					ServerId = server.Id,
					UserId = sub.UserId,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, alertedUsers, "User unsubscribe");
				}
			}
			else
			{
				var deletedRole = sub.SubscribeRoles.FirstOrDefault(usr => usr.RoleId == serverRole.Id);

				sub.SubscribeRoles.Remove(deletedRole);
				_hitsContext.UserServer.Update(sub);
				await _hitsContext.SaveChangesAsync();

				var removedChannels = await _hitsContext.ChannelCanSee
					.Where(ccs => ccs.RoleId == deletedRole.RoleId)
					.Select(ccs => ccs.ChannelId)
					.ToListAsync();
				foreach (var channelId in removedChannels)
				{
					bool stillHasAccess = await _hitsContext.ChannelCanSee
						.AnyAsync(ccs => removedChannels.Contains(ccs.ChannelId)
										 && sub.SubscribeRoles.Select(sr => sr.RoleId).Contains(ccs.RoleId));

					if (!stillHasAccess)
					{
						var lastRead = await _hitsContext.LastReadChannelMessage
							.FirstOrDefaultAsync(lr => lr.UserId == sub.UserId && lr.TextChannelId == channelId);

						if (lastRead != null)
							_hitsContext.LastReadChannelMessage.Remove(lastRead);
					}
				}
				await _hitsContext.SaveChangesAsync();

				var oldUserRole = new NewUserRoleResponseDTO
				{
					ServerId = server.Id,
					UserId = sub.UserId,
					RoleId = deletedRole.RoleId,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(oldUserRole, alertedUsers, "Role removed from user");
				}
			}
		}

		_hitsContext.Preset.Remove(preset);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task<ServerInvitationResponseDTO> CreateInvitationToken(string token, Guid serverId, DateTime expiresAt)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == owner.Id);
		if (ownerSub == null)
		{
			throw new CustomException("Owner is not subscriber of this server", "Check owner", "Owner", 404, "Пользователь не найден", "Генерация приглашения");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanUseInvitations) == false)
		{
			throw new CustomException("Owner does not have rights to use invitations", "Check user rights to use invitations", "Owner", 403, "Пользователь не имеет права генерировать приглашения", "Генерация приглашения");
		}

		var invitation = new ServerInvitationDbModel
		{
			ServerId = server.Id,
			UserId = owner.Id,
			Token = GenerateSecureToken(),
			ExpiresAt = expiresAt,
			IsRevoked = false,
		};

		await _hitsContext.Invitation.AddAsync(invitation);
		await _hitsContext.SaveChangesAsync();

		var invitationObject = new InvitationPayload
		{
			ServerName = server.Name,
			ServerIconId = server.IconFileId,
			InvitationToken = invitation.Token
		};

		var invitationJson = JsonSerializer.Serialize(invitationObject);
		var invitationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(invitationJson)).Replace("+", "-").Replace("/", "_").TrimEnd('=');

		var response = new ServerInvitationResponseDTO
		{
			InvitationString = invitationString
		};

		return response;
	}

	private static string GenerateSecureToken()
	{
		var bytes = RandomNumberGenerator.GetBytes(32);
		return Convert.ToBase64String(bytes)
			.Replace("+", "-")
			.Replace("/", "_")
			.TrimEnd('=');
	}
}
