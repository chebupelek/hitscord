using Authzed.Api.V0;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using hitscord.Redis.CashedDB;
using hitscord.Redis.CashedDB.Models;
using hitscord.WebSockets;
using Microsoft.EntityFrameworkCore;
using NickBuhro.Translit;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace hitscord.Services;

public class RolesService : IRolesService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authorizationService;
	private readonly IServerService _serverService;
	private readonly WebSocketsManager _webSocketManager;
	private readonly IRedisCacheService _cacheService;

	public RolesService(HitsContext hitsContext, IAuthorizationService authorizationService, IServerService serverService, WebSocketsManager webSocketManager, IRedisCacheService cacheService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
		_serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
	}


	private async Task<int> HashChannelRightsByRolesAsync(List<Guid> roleIds, Guid channelId)
	{
		ChannelRights rights = ChannelRights.None;

		if (await _hitsContext.ChannelCanSee
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.ChannelId == channelId))
		{
			rights |= ChannelRights.See;
		}

		if (await _hitsContext.ChannelCanWrite
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.TextChannelId == channelId))
		{
			rights |= ChannelRights.Write;
		}

		if (await _hitsContext.ChannelCanWriteSub
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.TextChannelId == channelId))
		{
			rights |= ChannelRights.WriteSub;
		}

		if (await _hitsContext.ChannelNotificated
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.NotificationChannelId == channelId))
		{
			rights |= ChannelRights.Notificate;
		}

		if (await _hitsContext.ChannelCanJoin
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.VoiceChannelId == channelId))
		{
			rights |= ChannelRights.Join;
		}

		if (await _hitsContext.ChannelCanUse
			.AnyAsync(x => roleIds.Contains(x.RoleId) && x.SubChannelId == channelId))
		{
			rights |= ChannelRights.Use;
		}

		return (int)rights;
	}

	private async Task UpdateChannelToUserByRolesAsync(Guid serverId, Guid channelId)
	{
		var users = await _hitsContext.UserServer
			.Where(us => us.ServerId == serverId)
			.SelectMany(us => us.SubscribeRoles
				.Select(sr => new
				{
					UserId = us.UserId,
					UserTag = us.User.AccountTag,
					UserNotifiable = us.User.Notifiable,
					UserServerId = us.Id,
					us.NonNotifiable,
					RoleId = sr.Role.Id,
					RoleTag = sr.Role.Tag
				}))
			.ToListAsync();

		var nonNotifiableChannels = await _hitsContext.NonNotifiableChannel
			.Where(n => n.TextChannelId == channelId)
			.Select(n => n.UserServerId)
			.ToListAsync();

		var nonNotifiableSet = nonNotifiableChannels.ToHashSet();

		var usersGrouped = users
			.GroupBy(x => x.UserId)
			.Select(g => new
			{
				UserId = g.Key,
				UserTag = g.First().UserTag,
				UserNotifiable = g.First().UserNotifiable,
				UserServerId = g.First().UserServerId,
				NonNotifiable = g.First().NonNotifiable,
				RoleIds = g.Select(x => x.RoleId).Distinct().ToList(),
				RoleTags = g.Select(x => x.RoleTag).Distinct().ToList()
			})
			.ToList();

		var tasks = usersGrouped.Select(u =>
		{
			int channelNotifiable =
				(u.UserNotifiable ? 1 : 0) +
				(u.NonNotifiable ? 1 : 0) +
				(!nonNotifiableSet.Contains(u.UserServerId) ? 1 : 0);

			return _cacheService.SetChannelToUserAsync(
				channelId,
				new ChannelToUserRedisFullDTO
				{
					UserId = u.UserId,
					Data = new ChannelToUserRedisItemDTO
					{
						UserTag = u.UserTag,
						RoleIds = u.RoleIds,
						RoleTags = u.RoleTags,
						ChannelNotifiable = channelNotifiable
					}
				});
		});

		await Task.WhenAll(tasks);
	}

	private async Task UpdateUserToChannelByRolesAsync(Guid serverId, Guid channelId)
	{
		var users = await _hitsContext.UserServer
			.Where(us => us.ServerId == serverId)
			.Select(us => new
			{
				UserId = us.UserId,
				RoleIds = us.SubscribeRoles.Select(sr => sr.RoleId).ToList()
			})
			.ToListAsync();

		if (users.Count == 0)
			return;

		var tasks = users.Select(async user =>
		{
			var rights = await HashChannelRightsByRolesAsync(user.RoleIds, channelId);

			await _cacheService.SetUserToChannelAsync(
				user.UserId,
				channelId,
				new UserToChannelRedisDTO
				{
					ChannelRights = rights
				}
			);
		});

		await Task.WhenAll(tasks);
	}

	private async Task UpdateCacheForUsersAsync(List<Guid> usersId, Guid serverId)
	{
		if (usersId == null || usersId.Count == 0)
			return;

		var channelsId = await _hitsContext.Channel
			.Where(c => c.ServerId == serverId)
			.Select(c => c.Id)
			.ToListAsync();

		var removeTasks = new List<Task>();

		foreach (var userId in usersId)
		{
			foreach (var channelId in channelsId)
			{
				removeTasks.Add(_cacheService.RemoveUserToChannelAsync(userId, channelId));
				removeTasks.Add(_cacheService.RemoveChannelToUserAsync(channelId, userId));
			}
		}

		await Task.WhenAll(removeTasks);

		await Task.WhenAll(
			channelsId.Select(channelId =>
				UpdateChannelToUserByRolesAsync(serverId, channelId)
			)
		);

		await Task.WhenAll(
			channelsId.Select(channelId =>
				UpdateUserToChannelByRolesAsync(serverId, channelId)
			)
		);
	}


	public async Task<RoleDbModel> CheckRoleAsync(Guid roleId, Guid serverId)
	{
		var dbCheck = await _hitsContext.Role
			.Include(r => r.ChannelCanSee)
			.Include(r => r.ChannelCanWrite)
			.Include(r => r.ChannelCanWriteSub)
			.Include(r => r.ChannelNotificated)
			.Include(r => r.ChannelCanUse)
			.Include(r => r.ChannelCanJoin)
			.FirstOrDefaultAsync(r => r.Id == roleId);
		if (dbCheck == null)
		{
			throw new CustomException("Role not found", "Check role for existing", "Role id", 404, "Роль не найдена", "Проверка наличия роли");
		}
		return dbCheck;
	}

	public async Task<RolesItemDTO> CreateRoleAsync(Guid UserId, Guid serverId, string roleName, string color)
	{
		var server = await _serverService.CheckServerExistAsync(serverId, false);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == UserId);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Create role", "User", 404, "Пользователь не является подписчиком сервера", "Создание роли");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateRoles) == false)
		{
			throw new CustomException("User does not have rights to create roles", "Create role", "User", 403, "Пользователь не имеет права создавать роли", "Создание роли");
		}

		var newRole = new RoleDbModel()
		{
			Name = roleName,
			Role = RoleEnum.Custom,
			ServerId = server.Id,
			Color = color,
			Tag = Regex.Replace(Transliteration.CyrillicToLatin(roleName, Language.Russian), "[^a-zA-Z0-9]", "").ToLower(),
			ServerCanChangeRole = false,
			ServerCanWorkChannels = false,
			ServerCanDeleteUsers = false,
			ServerCanMuteOther = false,
			ServerCanDeleteOthersMessages = false,
			ServerCanIgnoreMaxCount = false,
			ServerCanCreateRoles = false,
			ServerCanCreateLessons = false,
			ServerCanCheckAttendance = false,
			ServerCanUseInvitations = false,
			ChannelCanSee = new List<ChannelCanSeeDbModel>(),
			ChannelCanWrite = new List<ChannelCanWriteDbModel>(),
			ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>(),
			ChannelNotificated = new List<ChannelNotificatedDbModel>(),
			ChannelCanUse = new List<ChannelCanUseDbModel>(),
			ChannelCanJoin = new List<ChannelCanJoinDbModel>(),
		};

		await _hitsContext.Role.AddAsync(newRole);
		await _hitsContext.SaveChangesAsync();

		var roleResponse = new RolesItemDTO
		{
			Id = newRole.Id,
			ServerId = newRole.ServerId,
			Name = newRole.Name,
			Tag = newRole.Tag,
			Color = newRole.Color,
			Type = newRole.Role
		};

		var alertedUsers = await _cacheService.GetUsersInServerAsync(server.Id);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "New role");
		}

		return roleResponse;
	}

	public async Task DeleteRoleAsync(Guid UserId, Guid serverId, Guid roleId)
	{
		var server = await _serverService.CheckServerExistAsync(serverId, false);
		var role = await CheckRoleAsync(roleId, serverId);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == UserId);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Delete role", "User", 404, "Пользователь не является подписчиком сервера", "Удаление роли");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateRoles) == false)
		{
			throw new CustomException("User does not have rights to create roles", "Delete role", "User", 403, "Пользователь не имеет права удалять роли", "Удаление роли");
		}

		if (role.Role != RoleEnum.Custom)
		{
			throw new CustomException("Cant delete non custom role", "Delete role", "Role id", 400, "Нельзя удалить не пользовательскую роль", "Удаление роли");
		}

		var alertedUsers = await _cacheService.GetUsersInServerAsync(serverId);
		/*
		var userServers = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.Where(us => us.ServerId == server.Id && us.SubscribeRoles.Any(sr => sr.RoleId == role.Id))
			.ToListAsync();
		*/

		var userServers = await _hitsContext.UserServer
			.Where(us => us.ServerId == server.Id)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.Where(us => us.SubscribeRoles.Any(sr => sr.RoleId == role.Id))
			.ToListAsync();
		var usersId = userServers.Select(us => us.UserId).ToList();

		if (userServers != null && userServers.Count() > 0)
		{
			var uncertainRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Role == RoleEnum.Uncertain && r.ServerId == server.Id);
			if (uncertainRole == null)
			{
				throw new CustomException("Uncertain role not found", "Delete role", "Role id", 404, "Не найдена неопределенная роль", "Удаление роли");
			}
			var textChannels = await _hitsContext.TextChannel
				.Include(tc => tc.ChannelCanSee)
				.Where(tc => tc.ChannelCanSee.Any(ccs => ccs.RoleId == uncertainRole.Id) && EF.Property<string>(tc, "ChannelType") == "Text")
				.Select(tc => tc.Id)
				.ToListAsync();
			var notificationsChannels = await _hitsContext.NotificationChannel
				.Include(tc => tc.ChannelCanSee)
				.Where(tc => tc.ChannelCanSee.Any(ccs => ccs.RoleId == uncertainRole.Id))
				.Select(tc => tc.Id)
				.ToListAsync();
			var subChannels = await _hitsContext.SubChannel
				.Include(tc => tc.ChannelCanUse)
				.Where(tc => tc.ChannelCanUse.Any(ccs => ccs.RoleId == uncertainRole.Id))
				.Select(tc => tc.Id)
				.ToListAsync();
			var allChannels = textChannels
				.Concat(notificationsChannels)
				.Concat(subChannels)
				.Distinct()
				.ToList();

			var channelLastMessageIds = await _hitsContext.ChannelMessage
				.Where(m => allChannels.Contains((Guid)m.TextChannelId))
				.GroupBy(m => m.TextChannelId)
				.Select(g => new
				{
					ChannelId = g.Key,
					LastMessageId = g.Max(m => m.Id)
				})
				.ToDictionaryAsync(x => x.ChannelId, x => x.LastMessageId);

			foreach (var user in userServers)
			{

				var channelsThroughThisRole = user.SubscribeRoles
					.Where(sr => sr.RoleId == role.Id)
					.SelectMany(sr => sr.Role.ChannelCanSee.Select(ccs => ccs.ChannelId)
						.Concat(sr.Role.ChannelCanUse.Select(ccu => ccu.SubChannelId)))
					.Distinct()
					.ToList();

				var channelsThroughOtherRoles = user.SubscribeRoles
					.Where(sr => sr.RoleId != role.Id)
					.SelectMany(sr => sr.Role.ChannelCanSee.Select(ccs => ccs.ChannelId)
						.Concat(sr.Role.ChannelCanUse.Select(ccu => ccu.SubChannelId)))
					.Distinct()
					.ToList();

				var onlyThroughThisRole = channelsThroughThisRole
					.Except(channelsThroughOtherRoles)
					.ToList();

				await _hitsContext.LastReadChannelMessage
					.Where(lr => lr.UserId == user.UserId && onlyThroughThisRole.Contains(lr.TextChannelId))
					.ExecuteDeleteAsync();

				if (user.SubscribeRoles.Count() == 1)
				{
					await _hitsContext.SubscribeRole.AddAsync(new SubscribeRoleDbModel { UserServerId = user.Id, RoleId = uncertainRole.Id });
					await _hitsContext.SaveChangesAsync();

					await _webSocketManager.BroadcastMessageAsync(new NewUserRoleResponseDTO
					{
						ServerId = serverId,
						UserId = user.UserId,
						RoleId = uncertainRole.Id,
					}, alertedUsers, "Role added to user");

					var lastReads = await _hitsContext.LastReadChannelMessage
						.Where(lrcm => lrcm.UserId == user.UserId)
						.Select(lrcm => lrcm.TextChannelId)
						.ToArrayAsync();

					var missingChannels = allChannels.Except(lastReads).ToList();
					if (missingChannels.Count > 0)
					{
						var newLastReads = missingChannels.Select(chId => new LastReadChannelMessageDbModel
						{
							UserId = user.UserId,
							TextChannelId = chId,
							LastReadedMessageId = channelLastMessageIds.ContainsKey(chId)
								? channelLastMessageIds[chId]
								: 0
						}).ToList();

						await _hitsContext.LastReadChannelMessage.AddRangeAsync(newLastReads);
						await _hitsContext.SaveChangesAsync();
					}
				}

				var subRole = user.SubscribeRoles.FirstOrDefault(sr => sr.RoleId == role.Id);

				_hitsContext.SubscribeRole.Remove(subRole);
				await _hitsContext.SaveChangesAsync();

				await UpdateCacheForUsersAsync(usersId, serverId);

				await _webSocketManager.BroadcastMessageAsync(new NewUserRoleResponseDTO
				{
					ServerId = serverId,
					UserId = user.UserId,
					RoleId = role.Id,
				}, alertedUsers, "Role removed from user");
			}
		}

		_hitsContext.Role.Remove(role);
		await _hitsContext.SaveChangesAsync();

		var roleResponse = new DeleteRoleResposeDTO
		{
			ServerId = role.ServerId,
			RoleId = role.Id,
		};

		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "Deleted role");
		}
	}

	public async Task UpdateRoleAsync(Guid UserId, Guid serverId, Guid roleId, string name, string color)
	{
		var server = await _serverService.CheckServerExistAsync(serverId, false);
		var role = await CheckRoleAsync(roleId, serverId);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == UserId);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "UpdateRoleAsync", "User", 404, "Пользователь не является подписчиком сервера", "Обновление роли");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateRoles) == false)
		{
			throw new CustomException("User does not have rights to create roles", "UpdateRoleAsync", "User", 403, "Пользователь не имеет права удалять роли", "Обновление роли");
		}

		if (!Regex.IsMatch(color, "^#([A-Fa-f0-9]{6})$"))
		{
			throw new CustomException("Invalid color format", "UpdateRoleAsync", "Color", 400,"Неверный формат цвета. Используйте шестизначный HEX в формате #RRGGBB","Обновление роли");
		}

		if (role.Role == RoleEnum.Custom)
		{
			role.Name = name;
			role.Tag = Regex.Replace(Transliteration.CyrillicToLatin(name, Language.Russian), "[^a-zA-Z0-9]", "").ToLower();

			var channelsId = await _hitsContext.Channel
				.Where(c => c.ServerId == serverId)
				.Select(c => c.Id)
				.ToListAsync();
			foreach (var channelId in channelsId)
			{
				await _cacheService.UpdateRoleTagForAllUsersAsync(channelId, roleId, role.Tag);
			}
		}
		role.Color = color;

		_hitsContext.Role.Update(role);
		await _hitsContext.SaveChangesAsync();

		var roleResponse = new RolesItemDTO
		{
			Id = role.Id,
			ServerId = role.ServerId,
			Name = role.Name,
			Tag = role.Tag,
			Color = role.Color,
			Type = role.Role
		};

		var alertedUsers = await _cacheService.GetUsersInServerAsync(serverId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "Updated role");
		}
	}

	public async Task<RolesListDTO> GetServerRolesAsync(Guid UserId, Guid serverId)
	{
		var server = await _serverService.CheckServerExistAsync(serverId, true);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == UserId);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "GetServerRolesAsync", "User", 404, "Пользователь не является подписчиком сервера", "Получение ролей сервера");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateRoles) == false)
		{
			throw new CustomException("User does not have rights to create roles", "GetServerRolesAsync", "User", 403, "Пользователь не имеет права удалять роли", "Получение ролей сервера");
		}

		var rolesList = new List<RoleSettingsDTO>();

		foreach (var role in server.Roles)
		{
			rolesList.Add(new RoleSettingsDTO
			{
				Role = new RolesItemDTO
				{
					Id = role.Id,
					ServerId = server.Id,
					Name = role.Name,
					Tag = role.Tag,
					Color = role.Color,
					Type = role.Role
				},
				Settings = new SettingsDTO
				{
					CanChangeRole = role.ServerCanChangeRole,
					CanWorkChannels = role.ServerCanWorkChannels,
					CanDeleteUsers = role.ServerCanDeleteUsers,
					CanMuteOther = role.ServerCanMuteOther,
					CanDeleteOthersMessages = role.ServerCanDeleteOthersMessages,
					CanIgnoreMaxCount = role.ServerCanIgnoreMaxCount,
					CanCreateRoles = role.ServerCanCreateRoles,
					CanCreateLessons = role.ServerCanCreateLessons,
					CanCheckAttendance = role.ServerCanCheckAttendance,
					CanUseInvitations = role.ServerCanUseInvitations
				}
			});
		}

		return (new RolesListDTO { Roles = rolesList });
	}

	public async Task ChangeRoleSettingsAsync(Guid UserId, Guid serverId, Guid roleId, SettingsEnum setting, bool settingsData)
	{
		var server = await _serverService.CheckServerExistAsync(serverId, false);
		var role = await CheckRoleAsync(roleId, serverId);

		var ownerSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == UserId);
		if (ownerSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Change role settings", "User", 404, "Пользователь не является подписчиком сервера", "Изменение настроек роли");
		}
		if (ownerSub.SubscribeRoles.Any(sr => sr.Role.ServerCanCreateRoles) == false)
		{
			throw new CustomException("User does not have rights to create roles", "Change role settings", "User", 403, "Пользователь не имеет права удалять роли", "Изменение настроек роли");
		}

		if (role.Role != RoleEnum.Custom)
		{
			throw new CustomException("This role not custom", "Change role settings", "Role", 400, "Нельзя менять настройки предсозданных ролей", "Изменение настроек роли");
		}

		switch (setting)
		{
			case SettingsEnum.CanChangeRole:
				if (settingsData)
				{
					role.ServerCanChangeRole = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanChangeRole = false;
					role.ServerCanCreateRoles = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanWorkChannels:
				if (settingsData)
				{
					role.ServerCanWorkChannels = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanWorkChannels = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanDeleteUsers:
				if (settingsData)
				{
					role.ServerCanDeleteUsers = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanDeleteUsers = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanMuteOther:
				if (settingsData)
				{
					role.ServerCanMuteOther = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanMuteOther = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanDeleteOthersMessages:
				if (settingsData)
				{
					role.ServerCanDeleteOthersMessages = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanDeleteOthersMessages = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanIgnoreMaxCount:
				if (settingsData)
				{
					role.ServerCanIgnoreMaxCount = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanIgnoreMaxCount = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanCreateRole:
				if (settingsData)
				{
					role.ServerCanCreateRoles = true;
					role.ServerCanChangeRole = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanCreateRoles = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanCreateLessons:
				if (server.ServerType != ServerTypeEnum.Teacher)
				{
					throw new CustomException("Cant change CanCreateLessons", "Change role settings", "Role", 400, "Нельзя менять данную настройку в этом канале", "Изменение настроек роли");
				}
				if (settingsData)
				{
					role.ServerCanCreateLessons = true;
					role.ServerCanCheckAttendance = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanCreateLessons = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanCheckAttendance:
				if (server.ServerType != ServerTypeEnum.Teacher)
				{
					throw new CustomException("Cant change CanCreateLessons", "Change role settings", "Role", 400, "Нельзя менять данную настройку в этом канале", "Изменение настроек роли");
				}
				if (settingsData)
				{
					role.ServerCanCheckAttendance = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanCheckAttendance = false;
					role.ServerCanDeleteUsers = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanUseInvitations:
				if (settingsData)
				{
					role.ServerCanUseInvitations = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanUseInvitations = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			default: throw new CustomException("Setting not found", "Change role settings", "Setting", 404, "Настройка не найдена", "Изменение настроек роли");
		}

		var roleResponse = new RoleSettingsDTO
		{
			Role = new RolesItemDTO
			{
				Id = role.Id,
				ServerId = server.Id,
				Name = role.Name,
				Tag = role.Tag,
				Color = role.Color,
				Type = role.Role
			},
			Settings = new SettingsDTO
			{
				CanChangeRole = role.ServerCanChangeRole,
				CanWorkChannels = role.ServerCanWorkChannels,
				CanDeleteUsers = role.ServerCanDeleteUsers,
				CanMuteOther = role.ServerCanMuteOther,
				CanDeleteOthersMessages = role.ServerCanDeleteOthersMessages,
				CanIgnoreMaxCount = role.ServerCanIgnoreMaxCount,
				CanCreateRoles = role.ServerCanCreateRoles,
				CanCreateLessons = role.ServerCanCreateLessons,
				CanCheckAttendance = role.ServerCanCheckAttendance,
				CanUseInvitations = role.ServerCanUseInvitations
			}
		};

		var alertedUsers = await _cacheService.GetUsersInServerAsync(serverId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "Updated role settings");
		}
	}
}
