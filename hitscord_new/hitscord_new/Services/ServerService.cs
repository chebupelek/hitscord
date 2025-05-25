using EasyNetQ;
using Grpc.Net.Client.Balancer;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.OrientDb.Service;
using hitscord.WebSockets;
using HitscordLibrary.Models.other;
using HitscordLibrary.SocketsModels;
using Microsoft.EntityFrameworkCore;
using NickBuhro.Translit;
using System.Data;
using System.Text.RegularExpressions;

namespace hitscord.Services;

public class ServerService : IServerService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authorizationService;
    private readonly IServices.IAuthenticationService _authenticationService;
    private readonly OrientDbService _orientDbService;
	private readonly WebSocketsManager _webSocketManager;

	public ServerService(HitsContext hitsContext, IAuthorizationService authorizationService, IServices.IAuthenticationService authenticationService, OrientDbService orientDbService, WebSocketsManager webSocketManager)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
	}

    public async Task<ServerDbModel> CheckServerExistAsync(Guid serverId, bool includeChannels)
    {
        var server = includeChannels ? await _hitsContext.Server.Include(s => s.Roles).Include(s => s.Channels).FirstOrDefaultAsync(s => s.Id == serverId) :
            await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
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
                .Include(s => s.Channels)
                .Include(s => s.Roles)
                .FirstOrDefaultAsync(s => s.Id == serverId);
        if (server == null)
        {
            throw new CustomException("Server not found", "Get server with full model", "Server id", 404, "Сервер не найден", "Получение полной информации о сервере");
        }
        return server;
    }

    private async Task<RoleDbModel> CreateRoleAsync(Guid serverId, RoleEnum role, string roleName, string color)
    {
        var newRole = new RoleDbModel()
        {
            Name = roleName,
            Role = role,
            ServerId = serverId,
            Color = color,
            Tag = Regex.Replace(Transliteration.CyrillicToLatin(roleName, Language.Russian), "[^a-zA-Z0-9]", "").ToLower()
		};
        await _hitsContext.Role.AddAsync(newRole);
        await _hitsContext.SaveChangesAsync();
        return newRole;
    }

    public async Task<ServerIdDTO> CreateServerAsync(string token, string serverName)
    {
        var user = await _authorizationService.GetUserAsync(token);

        var newServer = new ServerDbModel()
        {
            Name = serverName
        };
        await _hitsContext.Server.AddAsync(newServer);
        await _hitsContext.SaveChangesAsync();

        var creatorRole = await CreateRoleAsync(newServer.Id, RoleEnum.Creator, "Создатель", "#FF0000");
        var adminRole = await CreateRoleAsync(newServer.Id, RoleEnum.Admin, "Админ", "#00FF00");
        var teacherRole = await CreateRoleAsync(newServer.Id, RoleEnum.Teacher, "Учитель", "#00FFFF");
        var studentRole = await CreateRoleAsync(newServer.Id, RoleEnum.Student, "Студент", 	"#FF00FF");
        var uncertainRole = await CreateRoleAsync(newServer.Id, RoleEnum.Uncertain, "Неопределенная", "#FFFF00");
        newServer.Roles = new List<RoleDbModel> { creatorRole, adminRole, teacherRole, studentRole, uncertainRole };
        _hitsContext.Server.Update(newServer);
        await _hitsContext.SaveChangesAsync();

        var newSub = new UserServerDbModel
        {
            UserId = user.Id,
            RoleId = creatorRole.Id,
            UserServerName = user.AccountName,
            IsBanned = false,
        };
        await _hitsContext.UserServer.AddAsync(newSub);
        await _hitsContext.SaveChangesAsync();

        var newTextChannel = new TextChannelDbModel
        {
            Name = "Основной текстовый",
            ServerId = newServer.Id,
            IsMessage = false,
        };
        var newVoiceChannel = new VoiceChannelDbModel
        {
            Name = "Основной голосовой",
            ServerId = newServer.Id,
			MaxCount = 999
        };
        await _hitsContext.Channel.AddAsync(newTextChannel);
        await _hitsContext.Channel.AddAsync(newVoiceChannel);
        await _hitsContext.SaveChangesAsync();

		await _orientDbService.CreateServerAsync(newServer.Id, user.Id, newTextChannel.Id, newVoiceChannel.Id, new List<RoleDbModel> { creatorRole, adminRole, teacherRole, studentRole, uncertainRole });

		newServer.Channels.Add(newTextChannel);
        newServer.Channels.Add(newVoiceChannel);
        _hitsContext.Server.Update(newServer);
        await _hitsContext.SaveChangesAsync();

		return (new ServerIdDTO { ServerId = newServer.Id });
    }

	public async Task SubscribeAsync(Guid serverId, string token, string? userName)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, true);
		await _authenticationService.CheckSubscriptionNotExistAsync(server.Id, user.Id);
		var existedSub = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == user.Id && us.Role.ServerId == serverId);
		if (existedSub != null && existedSub.IsBanned == true)
		{
			throw new CustomException("User banned in this server", "Subscribe", "User", 401, "Пользователь забанен на этом сервере", "Подписка");
		}
		var newSub = new UserServerDbModel
		{
			UserId = user.Id,
			RoleId = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Id,
			UserServerName = userName != null ? userName : user.AccountName,
			IsBanned = false
		};

		await _hitsContext.UserServer.AddAsync(newSub);
		await _hitsContext.SaveChangesAsync();

		await _orientDbService.AssignUserToRoleAsync(user.Id, server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain).Id);

		var newSubscriberResponse = new NewSubscribeResponseDTO
		{
			ServerId = serverId,
			UserId = user.Id,
			UserName = newSub.UserServerName,
			RoleId = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Id,
			RoleName = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Name,
			UserTag = user.AccountTag,
			Notifiable = user.Notifiable,
			FriendshipApplication = user.FriendshipApplication,
			NonFriendMessage = user.NonFriendMessage
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
		alertedUsers = alertedUsers.Where(a => a != user.Id).ToList();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newSubscriberResponse, alertedUsers, "New user on server");
		}
	}

	public async Task UnsubscribeAsync(Guid serverId, string token)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
		var subRole = await _authenticationService.CheckUserNotCreatorAsync(server.Id, user.Id);
		var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.RoleId == subRole.Id && us.UserId == user.Id);
		var userVoiceChannel = await _hitsContext.UserVoiceChannel
			.Include(us => us.VoiceChannel)
			.FirstOrDefaultAsync(us =>
				us.VoiceChannel.ServerId == server.Id
				&& us.UserId == user.Id);
		if (userVoiceChannel != null)
		{
			_hitsContext.UserVoiceChannel.Remove(userVoiceChannel);
		}
		_hitsContext.UserServer.Remove(sub);
		await _hitsContext.SaveChangesAsync();

		await _orientDbService.UnassignUserFromRoleAsync(user.Id, subRole.Id);

		var newUnsubscriberResponse = new UnsubscribeResponseDTO
		{
			ServerId = serverId,
			UserId = user.Id,
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
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
		var ownerSubRole = await _authenticationService.CheckUserCreatorAsync(server.Id, owner.Id);
		var newCreatorSubRole = await _authenticationService.CheckSubscriptionExistAsync(server.Id, newCreator.Id);
		var ownerSub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.RoleId == ownerSubRole.Id && us.UserId == owner.Id);
		var newCreatorSub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.RoleId == newCreatorSubRole.Id && us.UserId == newCreator.Id);
		var creatorRole = server.Roles.FirstOrDefault(s => s.Role == RoleEnum.Creator);
		var userVoiceChannel = await _hitsContext.UserVoiceChannel
			.Include(us => us.VoiceChannel)
			.FirstOrDefaultAsync(us =>
				us.VoiceChannel.ServerId == server.Id
				&& us.UserId == owner.Id);
		if (userVoiceChannel != null)
		{
			_hitsContext.UserVoiceChannel.Remove(userVoiceChannel);
		}
		await _orientDbService.UnassignUserFromRoleAsync(owner.Id, ownerSubRole.Id);
		_hitsContext.UserServer.Remove(ownerSub);
		await _orientDbService.UnassignUserFromRoleAsync(newCreator.Id, newCreatorSubRole.Id);
		await _orientDbService.AssignUserToRoleAsync(newCreator.Id, creatorRole.Id);
		newCreatorSub.RoleId = creatorRole.Id;
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
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
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
        await _authenticationService.CheckUserCreatorAsync(server.Id, owner.Id);
        var userServerRelations = _hitsContext.UserServer.Where(us => us.Role.ServerId == server.Id);
        var serverRoles = _hitsContext.Role.Where(r => r.ServerId == server.Id);
        _hitsContext.UserServer.RemoveRange(userServerRelations);
        _hitsContext.Role.RemoveRange(serverRoles);
        var voiceChannels = server.Channels.OfType<VoiceChannelDbModel>().ToList();
        foreach (var voiceChannel in voiceChannels)
        {
            voiceChannel.Users.Clear();
        }
        var channelsToDelete = server.Channels.ToList();
        _hitsContext.Channel.RemoveRange(channelsToDelete);
        _hitsContext.Server.Remove(server);
        await _hitsContext.SaveChangesAsync();

        await _orientDbService.DeleteServerAsync(server.Id);

        var serverDelete = new ServerDeleteDTO
        {
            ServerId = serverId
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {
			await _webSocketManager.BroadcastMessageAsync(serverDelete, alertedUsers, "Server deleted");
        }
    }

    public async Task<ServersListDTO> GetServerListAsync(string token)
    {
        var user = await _authorizationService.GetUserAsync(token);
        var idsList = await _orientDbService.GetSubscribedServerIdsListAsync(user.Id);
		var notifiableList = await _orientDbService.GetNonNotifiableServersForUserAsync(user.Id);
		return new ServersListDTO
        {
            ServersList = await _hitsContext.Server
                .Where(s => idsList.Contains(s.Id))
                .Select(s => new ServersListItemDTO
                {
                    ServerId = s.Id,
                    ServerName = s.Name,
					IsNotifiable = !notifiableList.Contains(s.Id)
                })
                .ToListAsync()
        };
    }

    public async Task ChangeUserRoleAsync(string token, Guid serverId, Guid userId, Guid roleId)
    {
        var owner = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, false);
        await _authenticationService.CheckUserRightsChangeRoles(server.Id, owner.Id);
        await _authorizationService.GetUserAsync(userId);
        var userSub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, userId);
        var userSubRoleId = await _orientDbService.GetUserRoleOnServerAsync(userId, serverId);
        if(userSubRoleId == null) 
        {
            throw new CustomException("User hasn't role", "Change user role", "User", 400, "У пользователя нет роли", "Изменение роли пользователя");
        }
        Guid userRoleId = (Guid)userSubRoleId;
        var userSubRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == userSubRoleId);
        if (userId == owner.Id)
        {
            throw new CustomException("User cant change his role", "Change user role", "User", 400, "Пользователь не может менять свою роль", "Изменение роли пользователя");
        }
        if (userSubRole.Role == RoleEnum.Creator)
        {
            throw new CustomException("User cant change role of creator", "Change user role", "User", 400, "Пользователь не может менять роль создателя сервера", "Изменение роли пользователя");
        }
        var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == serverId);

        var userServ = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == userId && us.RoleId == userSubRoleId);
        var newUserServ = new UserServerDbModel
        {
            UserId = userId,
            RoleId = role.Id,
            UserServerName = userServ.UserServerName,
            IsBanned = userServ.IsBanned
        };
		_hitsContext.UserServer.Remove(userServ);
        await _hitsContext.SaveChangesAsync();
		_hitsContext.UserServer.Add(newUserServ);
		await _hitsContext.SaveChangesAsync();

		await _orientDbService.UnassignUserFromRoleAsync(userId, userRoleId);
        await _orientDbService.AssignUserToRoleAsync(userId, role.Id);

        var newUserRole = new NewUserRoleResponseDTO
        {
            ServerId = serverId,
            UserId = userId,
            RoleId = role.Id,
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {
			await _webSocketManager.BroadcastMessageAsync(newUserRole, alertedUsers, "Role changed");
        }
    }

	public async Task<ServerInfoDTO> GetServerInfoAsync(string token, Guid serverId)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var server = await GetServerFullModelAsync(serverId);
		var sub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
		string result = await _orientDbService.GetUserRolePermissionsOnServerAsync(user.Id, serverId);

		var channelCanSee = await _orientDbService.GetVisibleChannelsAsync(user.Id, serverId);
		var channelCanWrite = await _orientDbService.GetWritableChannelsAsync(user.Id, serverId);
		var channelCanWriteSub = await _orientDbService.GetWritableSubChannelsAsync(user.Id, serverId);
		var channelCanNotificate = await _orientDbService.GetNotificatedChannelsAsync(user.Id, serverId);
		var channelCanJoin = await _orientDbService.GetJoinableChannelsAsync(user.Id, serverId);
		var notifiableServersList = await _orientDbService.GetNonNotifiableServersForUserAsync(user.Id);
		var notifiableChannelsList = await _orientDbService.GetNonNotifiableChannelsForUserAsync(user.Id, serverId);

		var voiceChannelResponses = await _hitsContext.VoiceChannel
			.Include(vc => vc.Users)
			.Where(vc => vc.ServerId == server.Id && channelCanSee.Contains(vc.Id))
			.Select(vc => new VoiceChannelResponseDTO
			{
				ChannelName = vc.Name,
				ChannelId = vc.Id,
				CanJoin = channelCanJoin.Contains(vc.Id),
				MaxCount = vc.MaxCount,
				Users = vc.Users.Select(u => new VoiceChannelUserDTO
				{
					UserId = u.UserId,
					MuteStatus = u.MuteStatus,
					IsStream = u.IsStream
				})
				.ToList()
			})
			.ToListAsync();

		var info = new ServerInfoDTO
		{
			ServerId = serverId,
			ServerName = server.Name,
			Roles = await _hitsContext.Role
				.Where(r => r.ServerId == server.Id)
				.Select(r => new RolesItemDTO
				{
					Id = r.Id,
					Name = r.Name,
					ServerId = r.ServerId,
					Tag = r.Tag,
					Color = r.Color
				})
				.ToListAsync(),
			UserRoleId = sub.Id,
			UserRole = sub.Name,
			IsCreator = sub.Role == RoleEnum.Creator,
			CanChangeRole = result.Contains("ServerCanChangeRole"),
			CanDeleteUsers = result.Contains("ServerCanDeleteUsers"),
			CanWorkWithChannels = result.Contains("ServerCanWorkChannels"),
			CanMuteOthers = result.Contains("ServerCanMuteOther"),
			CanDeleteOtherMessages = result.Contains("ServerCanDeleteOthersMessages"),
			IsNotifiable = !notifiableServersList.Contains(serverId),
			Users = await _hitsContext.UserServer
				.Include(us => us.Role)
				.Where(us => us.Role.ServerId == serverId)
				.Join(_hitsContext.User,
					  us => us.UserId,
					  u => u.Id,
					  (us, u) => new ServerUserDTO
					  {
						  UserId = u.Id,
						  UserName = u.AccountName,
						  UserTag = u.AccountTag,
						  RoleName = us.Role.Name,
						  Mail = u.Mail,
						  Notifiable = u.Notifiable,
						  FriendshipApplication = u.FriendshipApplication,
						  NonFriendMessage = u.NonFriendMessage
					  })
				.ToListAsync(),
			Channels = new ChannelListDTO
			{
				TextChannels = server.Channels
				.Where(c =>
					(
						channelCanSee.Contains(c.Id)
					) &&
					c is TextChannelDbModel &&
					((TextChannelDbModel)c).IsMessage == false)
				.Select(c => new TextChannelResponseDTO
				{
					ChannelName = c.Name,
					ChannelId = c.Id,
					CanWrite = channelCanWrite.Contains(c.Id),
					CanWriteSub = channelCanWriteSub.Contains(c.Id),
					IsNotifiable = !notifiableChannelsList.Contains(c.Id)
				})
				.ToList(),
				NotificationChannels = server.Channels
				.Where(c =>
					(
						channelCanSee.Contains(c.Id)
					) &&
					c is NotificationChannelDbModel)
				.Select(c => new NotificationChannelResponseDTO
				{
					ChannelName = c.Name,
					ChannelId = c.Id,
					CanWrite = channelCanWrite.Contains(c.Id),
					IsNotificated = channelCanNotificate.Contains(c.Id),
					IsNotifiable = !notifiableChannelsList.Contains(c.Id)
				})
				.ToList(),
				VoiceChannels = voiceChannelResponses
			}
		};

		return info;
	}

	public async Task DeleteUserFromServerAsync(string token, Guid serverId, Guid userId, string? banReason)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckUserRightsDeleteUsers(server.Id, owner.Id);
		await _authorizationService.GetUserAsync(userId);
		var userSub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, userId);
		if (userId == owner.Id)
		{
			throw new CustomException("User cant delete himself", "Change user role", "User", 400, "Пользователь не может удалить сам себя", "Удаление пользователя с сервера");
		}
		if (userSub.Role == RoleEnum.Creator)
		{
			throw new CustomException("User cant delete creator of server", "Change user role", "User", 400, "Нельзя удалить создателя сервера", "Удаление пользователя с сервера");
		}
		var userServer = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == userId && us.RoleId == userSub.Id);
		userServer.IsBanned = true;
		userServer.BanReason = banReason;
		userServer.BanTime = DateTime.UtcNow;
		_hitsContext.UserServer.Update(userServer);
		var userVoiceChannel = await _hitsContext.UserVoiceChannel.Include(uvc => uvc.VoiceChannel).FirstOrDefaultAsync(uvc => uvc.UserId == userId && uvc.VoiceChannel.ServerId == serverId);
		var newRemovedUserResponse = new RemovedUserDTO
		{
			ServerId = serverId,
			IsNeedRemoveFromVC = userVoiceChannel != null
		};
		await _hitsContext.SaveChangesAsync();
		await _orientDbService.UnassignUserFromRoleAsync(userId, userSub.Id);

		await _webSocketManager.BroadcastMessageAsync(newRemovedUserResponse, new List<Guid> { userId }, "You removed from server");

		var newUnsubscriberResponse = new UnsubscribeResponseDTO
		{
			ServerId = serverId,
			UserId = userId,
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, alertedUsers, "User unsubscribe");
		}
	}

	public async Task<RolesListDTO> GetServerRolesAsync(string token, Guid serverId)
    {
        var user = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, true);
        await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);

        var list = new RolesListDTO
        {
            Roles = server.Roles
                .Select(r => new RolesItemDTO
                {
                    Id = r.Id,
                    ServerId = server.Id,
                    Name = r.Name,
                    Tag = r.Tag,
                    Color = r.Color
                })
                .ToList()
        };

        return list;
    }

	public async Task ChangeServerNameAsync(Guid serverId, string token, string name)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckUserCreatorAsync(server.Id, owner.Id);
		server.Name = name;
		_hitsContext.Server.Update(server);
		await _hitsContext.SaveChangesAsync();

        var changeServerName = new ChangeNameDTO
        {
            Id = serverId,
            Name = name
        };
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changeServerName, alertedUsers, "New server name");
		}
	}

	public async Task ChangeUserNameAsync(Guid serverId, string token, string name)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckSubscriptionExistAsync(server.Id, owner.Id);
        var userServer = await _hitsContext.UserServer.Include(uvc => uvc.Role).FirstOrDefaultAsync(_uvc => _uvc.UserId == owner.Id && _uvc.Role.ServerId == serverId);
        if (userServer == null)
        {
			throw new CustomException("User not subscriber of this server", "Change user name", "User", 400, "Пользователь не является подписчикаом", "Изменение имени на сервере");
		}
        userServer.UserServerName = name;
		_hitsContext.UserServer.Update(userServer);
		await _hitsContext.SaveChangesAsync();


		var changeServerName = new ChangeNameOnServerDTO
		{
			ServerId = serverId,
            UserId = owner.Id,
			Name = name
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changeServerName, alertedUsers, "New users name on server");
		}
	}

	public async Task ChangeNonNotifiableServerAsync(string token, Guid serverId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckSubscriptionExistAsync(server.Id, owner.Id);

		await _orientDbService.ChangeNonNotifiableServer(owner.Id, server.Id);
	}

	public async Task<BanListDTO> GetBannedListAsync(string token, Guid serverId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckUserRightsDeleteUsers(server.Id, owner.Id);

		var bannedUsers = new BanListDTO
		{
			BannedList = await _hitsContext.UserServer
				.Include(us => us.User)
				.Include(us => us.Role)
				.Where(us => 
					us.Role.ServerId == server.Id
					&& us.IsBanned == true)
				.Select(us => new ServerBannedUserDTO
				{
					UserId = us.UserId,
					UserName = us.UserServerName,
					UserTag = us.User.AccountTag,
					Mail = us.User.Mail,
					BanReason = us.BanReason,
					BanTime = (DateTime)us.BanTime
				})
				.ToListAsync()
		};

		return bannedUsers;
	}

	public async Task UnBanUser(string token, Guid serverId, Guid bannedId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckUserRightsDeleteUsers(server.Id, owner.Id);

		var banned = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.Role.ServerId == serverId && us.UserId == bannedId && us.IsBanned == true);
		if (banned == null)
		{
			throw new CustomException("Banned user not found", "Unban user", "User", 404, "Забаненный пользователь не найден", "Разбан пользователя");
		}

		_hitsContext.UserServer.Remove(banned);
		await _hitsContext.SaveChangesAsync();
	}
}
