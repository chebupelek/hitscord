﻿using Authzed.Api.V0;
using EasyNetQ;
using Grpc.Core;
using Grpc.Net.Client.Balancer;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.request;
using hitscord.Models.response;
using hitscord.OrientDb.Service;
using hitscord.WebSockets;
using hitscord_new.Migrations;
using HitscordLibrary.Contexts;
using HitscordLibrary.Models;
using HitscordLibrary.Models.db;
using HitscordLibrary.Models.other;
using HitscordLibrary.nClamUtil;
using HitscordLibrary.SocketsModels;
using Microsoft.EntityFrameworkCore;
using nClam;
using NickBuhro.Translit;
using System;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace hitscord.Services;

public class ServerService : IServerService
{
    private readonly HitsContext _hitsContext;
	private readonly FilesContext _filesContext;
	private readonly IAuthorizationService _authorizationService;
    private readonly IServices.IAuthenticationService _authenticationService;
    private readonly OrientDbService _orientDbService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly nClamService _clamService;
	private readonly INotificationService _notificationsService;

	public ServerService(HitsContext hitsContext, IAuthorizationService authorizationService, IServices.IAuthenticationService authenticationService, OrientDbService orientDbService, WebSocketsManager webSocketManager, nClamService clamService, FilesContext filesContext, INotificationService notificationsService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
		_filesContext = filesContext ?? throw new ArgumentNullException(nameof(filesContext));
		_notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
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

	public async Task<FileMetaResponseDTO?> GetImageAsync(Guid iconId)
	{
		var file = await _filesContext.File.FindAsync(iconId);
		if (file == null)
			return null;

		if (!file.Type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			return null;

		return new FileMetaResponseDTO
		{
			FileId = file.Id,
			FileName = file.Name,
			FileType = file.Type,
			FileSize = file.Size
		};
	}

	public async Task<ServerIdDTO> CreateServerAsync(string token, string serverName)
    {
        var user = await _authorizationService.GetUserAsync(token);

        var newServer = new ServerDbModel()
        {
            Name = serverName,
			IconId = null,
			IsClosed = false
        };
        await _hitsContext.Server.AddAsync(newServer);
        await _hitsContext.SaveChangesAsync();

        var creatorRole = await CreateRoleAsync(newServer.Id, RoleEnum.Creator, "Создатель", "#FF0000");
        var adminRole = await CreateRoleAsync(newServer.Id, RoleEnum.Admin, "Админ", "#00FF00");
        var uncertainRole = await CreateRoleAsync(newServer.Id, RoleEnum.Uncertain, "Неопределенная", "#FFFF00");
        newServer.Roles = new List<RoleDbModel> { creatorRole, adminRole, uncertainRole };
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

		await _orientDbService.CreateServerAsync(newServer.Id, user.Id, newTextChannel.Id, newVoiceChannel.Id, new List<RoleDbModel> { creatorRole, adminRole, uncertainRole });

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
		if (server.IsClosed == false)
		{
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

			var newSubscriberResponse = new ServerUserDTO
			{
				ServerId = serverId,
				UserId = user.Id,
				UserName = user.AccountName,
				UserTag = user.AccountTag,
				Icon = null,
				RoleId = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Id,
				RoleName = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Name,
				RoleType = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Role,
				Mail = user.Mail,
				Notifiable = user.Notifiable,
				FriendshipApplication = user.FriendshipApplication,
				NonFriendMessage = user.NonFriendMessage,
				isFriend = false
			};
			if (user != null && user.IconId != null)
			{
				var userIcon = await GetImageAsync((Guid)user.IconId);
				newSubscriberResponse.Icon = userIcon;
			}

			var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
			alertedUsers = alertedUsers.Where(a => a != user.Id).ToList();
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				foreach (var alertedUser in alertedUsers)
				{
					newSubscriberResponse.isFriend = await _orientDbService.AreUsersFriendsAsync(user.Id, alertedUser);
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
				ServerUserName = userName,
				CreatedAt = DateTime.UtcNow
			};

			await _hitsContext.ServerApplications.AddAsync(newApplication);
			await _hitsContext.SaveChangesAsync();
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
		if (ownerSub == null)
		{
			throw new CustomException("Owner sub not found", "Unsubscribe for creator", "Owner subscription", 404, "Подписка создателя не найдена", "Отписка для создателя");
		}
		var newCreatorSub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.RoleId == newCreatorSubRole.Id && us.UserId == newCreator.Id && us.IsBanned == false);
		if (newCreatorSub == null)
		{
			throw new CustomException("New creator sub not found", "Unsubscribe for creator", "New creator subscription", 404, "Подписка нового создателя не найдена", "Отписка для создателя");
		}
		var creatorRole = server.Roles.FirstOrDefault(s => s.Role == RoleEnum.Creator);
		if ((creatorRole == null) || (await _orientDbService.RoleExistsOnServerAsync(creatorRole.Id, serverId) == false))
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
		await _orientDbService.UnassignUserFromRoleAsync(owner.Id, ownerSubRole.Id);
		_hitsContext.UserServer.Remove(ownerSub);
		await _orientDbService.UnassignUserFromRoleAsync(newCreator.Id, newCreatorSubRole.Id);
		await _orientDbService.AssignUserToRoleAsync(newCreator.Id, creatorRole.Id);
		var newCreatorNewSub = new UserServerDbModel
		{
			UserId = newCreatorSub.UserId,
			RoleId = creatorRole.Id,
			UserServerName = newCreatorSub.UserServerName,
			IsBanned = newCreatorSub.IsBanned
		};
		_hitsContext.UserServer.Remove(newCreatorSub);
		await _hitsContext.SaveChangesAsync();
		_hitsContext.UserServer.Add(newCreatorNewSub);
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
			await _notificationsService.AddNotificationForUsersListAsync(alertedUsers, $"Сервер {server.Name} был удален");
		}
	}

    public async Task<ServersListDTO> GetServerListAsync(string token)
    {
        var user = await _authorizationService.GetUserAsync(token);
        var idsList = await _orientDbService.GetSubscribedServerIdsListAsync(user.Id);
		var notifiableList = await _orientDbService.GetNonNotifiableServersForUserAsync(user.Id);

		var freshList = await _hitsContext.Server
				.Where(s => idsList.Contains(s.Id))
				.ToListAsync();

		var serverList = new List<ServersListItemDTO>();

		foreach (var server in freshList)
		{
			var icon = server.IconId == null ? null : await GetImageAsync((Guid)server.IconId);
			serverList.Add(new ServersListItemDTO
			{
				ServerId = server.Id,
				ServerName = server.Name,
				IsNotifiable = !notifiableList.Contains(server.Id),
				Icon = icon
			});
		}

		return (new ServersListDTO
        {
            ServersList = serverList
		});
    }

    public async Task ChangeUserRoleAsync(string token, Guid serverId, Guid userId, Guid roleId)
    {
        var owner = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, false);
        await _authenticationService.CheckUserRightsChangeRoles(server.Id, owner.Id);
        await _authorizationService.GetUserAsync(userId);
		var ownerSub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, owner.Id);
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
		if (ownerSub.Role > userSub.Role)
		{
			throw new CustomException("Owner lower in ierarchy than changed user", "Change user role", "Changed user role", 401, "Пользователь ниже по иерархии чем изменяемый пользователь", "Изменение роли пользователя");
		}

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == serverId && r.Role != RoleEnum.Creator);

		if ((role == null) || (await _orientDbService.RoleExistsOnServerAsync(roleId, serverId) == false))
		{
			throw new CustomException("Role not found", "Change user role", "Role ID", 404, "Роль не найдена", "Изменение роли пользователя");
		}
		if (ownerSub.Role > role.Role)
		{
			throw new CustomException("Owner lower in ierarchy than changed role", "Change role", "Changed user role", 401, "Пользователь ниже по иерархии чем назначаемая роль", "Изменение роли пользователя");
		}

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
		var friendsIds = await _orientDbService.GetFriendsByUserIdAsync(user.Id);

		var icon = server.IconId == null ? null : await GetImageAsync((Guid)server.IconId);

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

		var pairVoiceChannelResponses = await _hitsContext.PairVoiceChannel
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

		var serverUsers = await _hitsContext.UserServer
				.Include(us => us.Role)
				.Where(us => us.Role.ServerId == serverId)
				.Join(_hitsContext.User,
					  us => us.UserId,
					  u => u.Id,
					  (us, u) => new ServerUserDTO
					  {
						  ServerId = serverId,
						  UserId = u.Id,
						  UserName = us.UserServerName,
						  UserTag = u.AccountTag,
						  Icon = null,
						  RoleId = us.RoleId,
						  RoleName = us.Role.Name,
						  RoleType = us.Role.Role,
						  Mail = u.Mail,
						  Notifiable = u.Notifiable,
						  FriendshipApplication = u.FriendshipApplication,
						  NonFriendMessage = u.NonFriendMessage,
						  isFriend = friendsIds.Contains(u.Id)
					  })
				.ToListAsync();

		foreach (var serverUser in serverUsers)
		{
			var userEntity = await _hitsContext.User.FindAsync(serverUser.UserId);
			if (userEntity != null && userEntity.IconId != null)
			{
				var userIcon = await GetImageAsync((Guid)userEntity.IconId);
				serverUser.Icon = userIcon;
			}
			else
			{
				serverUser.Icon = null;
			}
		}

		var info = new ServerInfoDTO
		{
			ServerId = serverId,
			ServerName = server.Name,
			Icon = icon,
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
			UserRoleId = sub.Id,
			UserRole = sub.Name,
			UserRoleType = sub.Role,
			IsCreator = sub.Role == RoleEnum.Creator,
			Permissions = new SettingsDTO
			{
				CanChangeRole = result.Contains("ServerCanChangeRole"),
				CanWorkChannels = result.Contains("ServerCanWorkChannels"),
				CanDeleteUsers = result.Contains("ServerCanDeleteUsers"),
				CanMuteOther = result.Contains("ServerCanMuteOther"),
				CanDeleteOthersMessages = result.Contains("ServerCanDeleteOthersMessages"),
				CanIgnoreMaxCount = result.Contains("ServerCanIgnoreMaxCount"),
				CanCreateRoles = result.Contains("ServerCanCreateRoles"),
				CanCreateLessons = result.Contains("ServerCanCreateLessons"),
				CanCheckAttendance = result.Contains("ServerCanCheckAttendance")
			},
			IsNotifiable = !notifiableServersList.Contains(serverId),
			Users = serverUsers,
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
				VoiceChannels = voiceChannelResponses,
				PairVoiceChannels = pairVoiceChannelResponses
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
		var ownerSub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, owner.Id);
		var userSub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, userId);
		if (userId == owner.Id)
		{
			throw new CustomException("User cant delete himself", "Delete user from server", "User", 400, "Пользователь не может удалить сам себя", "Удаление пользователя с сервера");
		}
		if (userSub.Role == RoleEnum.Creator)
		{
			throw new CustomException("User cant delete creator of server", "Delete user from server", "User", 400, "Нельзя удалить создателя сервера", "Удаление пользователя с сервера");
		}
		if ((ownerSub.Role > userSub.Role))
		{
			throw new CustomException("Owner lower in ierarchy than deleted user", "Delete user from server", "Changed user role", 401, "Пользователь ниже по иерархии чем удаляемый пользователь", "Удаление пользователя с сервера");
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
		await _notificationsService.AddNotificationForUserAsync(userId, $"Вы были забанены на сервере: {server.Name}");
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

	public async Task<BanListDTO> GetBannedListAsync(string token, Guid serverId, int page, int size)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckUserRightsDeleteUsers(server.Id, owner.Id);
		var bannedCount = await _hitsContext.UserServer.Where(us => us.Role.ServerId == server.Id && us.IsBanned == true).CountAsync();
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
		await _authenticationService.CheckUserRightsDeleteUsers(server.Id, owner.Id);

		var banned = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.Role.ServerId == serverId && us.UserId == bannedId && us.IsBanned == true);
		if (banned == null)
		{
			throw new CustomException("Banned user not found", "Unban user", "User", 404, "Забаненный пользователь не найден", "Разбан пользователя");
		}

		_hitsContext.UserServer.Remove(banned);
		await _hitsContext.SaveChangesAsync();

		await _notificationsService.AddNotificationForUserAsync(banned.UserId, $"Вы были разбанены на сервере: {server.Name}");
	}

	public async Task ChangeServerIconAsync(string token, Guid serverId, IFormFile iconFile)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckUserCreatorAsync(serverId, owner.Id);

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
		originalFileName = Path.GetFileName(originalFileName);
		var iconDirectory = Path.Combine("wwwroot", "icons");

		Directory.CreateDirectory(iconDirectory);

		var iconPath = Path.Combine(iconDirectory, originalFileName);

		await File.WriteAllBytesAsync(iconPath, fileBytes);

		if (server.IconId != null)
		{
			var oldIcon = await _filesContext.File.FirstOrDefaultAsync(f => f.Id == server.IconId);
			if (oldIcon != null)
			{
				var oldIconPath = Path.Combine("wwwroot", oldIcon.Path.TrimStart('/'));

				if (File.Exists(oldIconPath))
				{
					File.Delete(oldIconPath);
				}

				_filesContext.File.Remove(oldIcon);
			}
		}

		var file = new FileDbModel
		{
			Id = Guid.NewGuid(),
			Path = $"/icons/{originalFileName}",
			Name = originalFileName,
			Type = iconFile.ContentType,
			Size = iconFile.Length,
			Creator = owner.Id,
			IsApproved = true,
			CreatedAt = DateTime.UtcNow,
		};

		_filesContext.File.Add(file);
		await _filesContext.SaveChangesAsync();

		server.IconId = file.Id;
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
				FileSize = file.Size
			}
		};

		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
		if (alertedUsers != null && alertedUsers.Any())
		{
			await _webSocketManager.BroadcastMessageAsync(changeIconDto, alertedUsers, "New icon on server");
		}
	}

	public async Task ChangeServerClosedAsync(string token, Guid serverId, bool isClosed, bool? isApproved)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckUserCreatorAsync(serverId, owner.Id);

		if (server.IsClosed == isClosed)
		{
			throw new CustomException($"Server isClosed is already {isClosed}", "Сhange server isClosed", "isClosed", 400, $"Закрытость сервера уже {isClosed}", "Изменение закрытости сервера");
		}

		server.IsClosed = isClosed;

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
					foreach (var application in applications)
					{
						var user = await _authorizationService.GetUserAsync(application.UserId);
						var newSub = new UserServerDbModel
						{
							UserId = application.UserId,
							RoleId = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Id,
							UserServerName = application.ServerUserName != null ? application.ServerUserName : user.AccountName,
							IsBanned = false
						};

						await _hitsContext.UserServer.AddAsync(newSub);
						_hitsContext.ServerApplications.Remove(application);
						await _hitsContext.SaveChangesAsync();
						await _orientDbService.AssignUserToRoleAsync(user.Id, server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain).Id);

						var newSubscriberResponse = new NewSubscribeResponseDTO
						{
							ServerId = server.Id,
							UserId = user.Id,
							UserName = newSub.UserServerName,
							RoleId = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Id,
							RoleName = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Name,
							UserTag = user.AccountTag,
							Notifiable = user.Notifiable,
							FriendshipApplication = user.FriendshipApplication,
							NonFriendMessage = user.NonFriendMessage
						};
						var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(server.Id);
						alertedUsers = alertedUsers.Where(a => a != user.Id).ToList();
						if (alertedUsers != null && alertedUsers.Count() > 0)
						{
							await _webSocketManager.BroadcastMessageAsync(newSubscriberResponse, alertedUsers, "New user on server");
						}
						await _notificationsService.AddNotificationForUserAsync(application.UserId, $"Вашу заявку на присоединение к серверу {server.Name} приняли");
					}
				}
				else
				{
					_hitsContext.ServerApplications.RemoveRange(applications);
					await _hitsContext.SaveChangesAsync();
					await _notificationsService.AddNotificationForUsersListAsync(applications.Select(a => a.UserId).ToList(), $"Вашу заявку на присоединение к серверу {server.Name} отклонили");
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
		await _authenticationService.CheckUserRightsDeleteUsers(application.ServerId, owner.Id);
		var newSub = new UserServerDbModel
		{
			UserId = application.UserId,
			RoleId = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Id,
			UserServerName = application.ServerUserName != null ? application.ServerUserName : user.AccountName,
			IsBanned = false
		};

		await _hitsContext.UserServer.AddAsync(newSub);
		_hitsContext.ServerApplications.Remove(application);
		await _hitsContext.SaveChangesAsync();
		await _orientDbService.AssignUserToRoleAsync(user.Id, server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain).Id);

		var newSubscriberResponse = new NewSubscribeResponseDTO
		{
			ServerId = server.Id,
			UserId = user.Id,
			UserName = newSub.UserServerName,
			RoleId = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Id,
			RoleName = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Name,
			UserTag = user.AccountTag,
			Notifiable = user.Notifiable,
			FriendshipApplication = user.FriendshipApplication,
			NonFriendMessage = user.NonFriendMessage
		};
		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(server.Id);
		alertedUsers = alertedUsers.Where(a => a != user.Id).ToList();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newSubscriberResponse, alertedUsers, "New user on server");
		}
		await _notificationsService.AddNotificationForUserAsync(application.UserId, $"Вашу заявку на присоединение к серверу {server.Name} приняли");
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
		await _authenticationService.CheckUserRightsDeleteUsers(server.Id, owner.Id);

		_hitsContext.ServerApplications.Remove(application);
		await _hitsContext.SaveChangesAsync();

		await _notificationsService.AddNotificationForUserAsync(application.UserId, $"Вашу заявку на присоединение к серверу {server.Name} отклонили");
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
		await _authenticationService.CheckUserRightsDeleteUsers(server.Id, owner.Id);
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
			.Select(sa => new ServerApplicationResponseDTO
			{
				ApplicationId = sa.Id,
				ServerId = sa.ServerId,
				User = new ProfileDTO
				{
					Id = sa.User.Id,
					Name = sa.User.AccountName,
					Tag = sa.User.AccountTag,
					Mail = sa.User.Mail,
					AccontCreateDate = DateOnly.FromDateTime(sa.User.AccountCreateDate),
					Notifiable = sa.User.Notifiable,
					FriendshipApplication = sa.User.FriendshipApplication,
					NonFriendMessage = sa.User.NonFriendMessage,
					Icon = null
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
}
