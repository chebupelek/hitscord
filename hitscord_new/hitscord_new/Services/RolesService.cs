﻿using EasyNetQ;
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
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.RegularExpressions;
using Grpc.Core;
using Authzed.Api.V0;

namespace hitscord.Services;

public class RolesService : IRolesService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authorizationService;
	private readonly IServerService _serverService;
	private readonly IServices.IAuthenticationService _authenticationService;
    private readonly OrientDbService _orientDbService;
	private readonly WebSocketsManager _webSocketManager;

	public RolesService(HitsContext hitsContext, IAuthorizationService authorizationService, IServerService serverService, IServices.IAuthenticationService authenticationService, OrientDbService orientDbService, WebSocketsManager webSocketManager)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
		_serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
		_authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
	}

	public async Task<RoleDbModel> CheckRoleAsync(Guid roleId, Guid serverId)
	{
		var orientCheck = await _orientDbService.RoleExistsOnServerAsync(roleId, serverId);
		if (orientCheck == false)
		{
			throw new CustomException("Role not found in OrientDb", "Check role for existing", "Role id", 404, "Роль не найдена", "Проверка наличия роли");
		}
		var dbCheck = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId);
		if (dbCheck == null)
		{
			throw new CustomException("Role not found in postgres", "Check role for existing", "Role id", 404, "Роль не найдена", "Проверка наличия роли");
		}
		return dbCheck;
	}

	public async Task<RolesItemDTO> CreateRoleAsync(string token, Guid serverId, string roleName, string color)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await _serverService.CheckServerExistAsync(serverId, false);
		await _authenticationService.CheckUserRightsCreateRoles(server.Id, owner.Id);

		var newRole = new RoleDbModel()
		{
			Name = roleName,
			Role = RoleEnum.Custom,
			ServerId = server.Id,
			Color = color,
			Tag = Regex.Replace(Transliteration.CyrillicToLatin(roleName, Language.Russian), "[^a-zA-Z0-9]", "").ToLower()
		};

		await _hitsContext.Role.AddAsync(newRole);
		await _orientDbService.AddRoleAsync(newRole.Id, newRole.Name, newRole.Tag, serverId, newRole.Color, (int)newRole.Role);
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

		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "New role");
		}

		return roleResponse;
	}

	public async Task DeleteRoleAsync(string token, Guid serverId, Guid roleId)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await _serverService.CheckServerExistAsync(serverId, false);
		var role = await CheckRoleAsync(roleId, serverId);
		await _authenticationService.CheckUserRightsCreateRoles(server.Id, owner.Id);

		var users = await _orientDbService.GetUsersSubscribedToRoleAsync(roleId);
		if (role.Role != RoleEnum.Custom)
		{
			throw new CustomException("Cant delete non custom role", "Delete role", "Role id", 400, "Нельзя удалить не пользовательскую роль", "Удаление роли");
		}
		if (users != null && users.Count() > 0)
		{
			var uncertainRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Role == RoleEnum.Uncertain && r.ServerId == server.Id);
			if (uncertainRole == null)
			{
				throw new CustomException("Uncertain role not found", "Delete role", "Role id", 404, "Не найдена неопределенная роль", "Удаление роли");
			}

			foreach (var user in users)
			{
				var userServ = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == user && us.RoleId == roleId);
				if (userServ != null)
				{
					var newUserServ = new UserServerDbModel
					{
						UserId = user,
						RoleId = uncertainRole.Id,
						UserServerName = userServ.UserServerName,
						IsBanned = userServ.IsBanned
					};
					_hitsContext.UserServer.Remove(userServ);
					await _hitsContext.SaveChangesAsync();
					_hitsContext.UserServer.Add(newUserServ);
					await _hitsContext.SaveChangesAsync();

					await _orientDbService.UnassignUserFromRoleAsync(user, role.Id);
					await _orientDbService.AssignUserToRoleAsync(user, uncertainRole.Id);

					var newUserRole = new NewUserRoleResponseDTO
					{
						ServerId = serverId,
						UserId = user,
						RoleId = uncertainRole.Id,
					};
					var alertedUsersUncertain = await _orientDbService.GetUsersByServerIdAsync(serverId);
					if (alertedUsersUncertain != null && alertedUsersUncertain.Count() > 0)
					{
						await _webSocketManager.BroadcastMessageAsync(newUserRole, alertedUsersUncertain, "Role changed");
					}
				}
			}
		}

		await _orientDbService.DeleteRoleAsync(role.Id);
		_hitsContext.Role.Remove(role);
		await _hitsContext.SaveChangesAsync();

		var roleResponse = new DeleteRoleResposeDTO
		{
			ServerId = role.ServerId,
			RoleId = role.Id,
		};

		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "Deleted role");
		}
	}

	public async Task UpdateRoleAsync(string token, Guid serverId, Guid roleId, string name, string color)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await _serverService.CheckServerExistAsync(serverId, false);
		var role = await CheckRoleAsync(roleId, serverId);
		await _authenticationService.CheckUserRightsCreateRoles(server.Id, owner.Id);

		if (!Regex.IsMatch(color, "^#([A-Fa-f0-9]{6})$"))
		{
			throw new CustomException("Invalid color format", "UpdateRoleAsync", "Color", 400,"Неверный формат цвета. Используйте шестизначный HEX в формате #RRGGBB","Обновление роли");
		}

		if (role.Role == RoleEnum.Custom)
		{
			role.Name = name;
			role.Tag = Regex.Replace(Transliteration.CyrillicToLatin(name, Language.Russian), "[^a-zA-Z0-9]", "").ToLower();
		}
		role.Color = color;

		await _orientDbService.UpdateRoleAsync(role.Id, role.Name, role.Tag, role.Color);
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

		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "Updated role");
		}
	}

	public async Task<RolesListDTO> GetServerRolesAsync(string token, Guid serverId)
	{
		var user = await _authorizationService.GetUserAsync(token);
		var server = await _serverService.CheckServerExistAsync(serverId, true);
		await _authenticationService.CheckUserRightsCreateRoles(server.Id, user.Id);

		var rolesList = new List<RoleSettingsDTO>();

		foreach (var role in server.Roles)
		{
			var permissions = await _orientDbService.GetRolePermissionsOnServerAsync(role.Id, server.Id);
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
					CanChangeRole = permissions.Contains("ServerCanChangeRole"),
					CanWorkChannels = permissions.Contains("ServerCanWorkChannels"),
					CanDeleteUsers = permissions.Contains("ServerCanDeleteUsers"),
					CanMuteOther = permissions.Contains("ServerCanMuteOther"),
					CanDeleteOthersMessages = permissions.Contains("ServerCanDeleteOthersMessages"),
					CanIgnoreMaxCount = permissions.Contains("ServerCanIgnoreMaxCount"),
					CanCreateRoles = permissions.Contains("ServerCanCreateRoles"),
					CanCreateLessons = permissions.Contains("ServerCanCreateLessons"),
					CanCheckAttendance = permissions.Contains("ServerCanCheckAttendance")
				}
			});
		}

		return (new RolesListDTO { Roles = rolesList });
	}

	public async Task ChangeRoleSettingsAsync(string token, Guid serverId, Guid roleId, SettingsEnum setting, bool settingsData)
	{
		var owner = await _authorizationService.GetUserAsync(token);
		var server = await _serverService.CheckServerExistAsync(serverId, false);
		var role = await CheckRoleAsync(roleId, serverId);
		await _authenticationService.CheckUserRightsCreateRoles(server.Id, owner.Id);

		if (role.Role != RoleEnum.Custom)
		{
			throw new CustomException("This role not custom", "Change role settings", "Role", 400, "Нельзя менять настройки предсозданных ролей", "Изменение настроек роли");
		}

		switch (setting)
		{
			case SettingsEnum.CanChangeRole:
				if (settingsData)
				{
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanChangeRole");
				}
				else
				{
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanChangeRole");
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanCreateRole");
				}
				break;

			case SettingsEnum.CanWorkChannels:
				if (settingsData)
				{
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanWorkChannels");
				}
				else
				{
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanWorkChannels");
				}
				break;

			case SettingsEnum.CanDeleteUsers:
				if (settingsData)
				{
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanDeleteUsers");
				}
				else
				{
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanDeleteUsers");
				}
				break;

			case SettingsEnum.CanMuteOther:
				if (settingsData)
				{
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanMuteOther");
				}
				else
				{
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanMuteOther");
				}
				break;

			case SettingsEnum.CanDeleteOthersMessages:
				if (settingsData)
				{
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanDeleteOthersMessages");
				}
				else
				{
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanDeleteOthersMessages");
				}
				break;

			case SettingsEnum.CanIgnoreMaxCount:
				if (settingsData)
				{
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanIgnoreMaxCount");
				}
				else
				{
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanIgnoreMaxCount");
				}
				break;

			case SettingsEnum.CanCreateRole:
				if (settingsData)
				{
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanCreateRole");
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanChangeRole");
				}
				else
				{
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanCreateRole");
				}
				break;

			case SettingsEnum.CanCreateLessons:
				if (settingsData)
				{
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanCreateLessons");
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanCheckAttendance");
				}
				else
				{
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanCreateLessons");
				}
				break;

			case SettingsEnum.CanCheckAttendance:
				if (settingsData)
				{
					await _orientDbService.GrantRolePermissionToServerAsync(role.Id, server.Id, "ServerCanCheckAttendance");
				}
				else
				{
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanCheckAttendance");
					await _orientDbService.RevokeRolePermissionFromServerAsync(role.Id, server.Id, "ServerCanCreateLessons");
				}
				break;

			default: throw new CustomException("Setting not found", "Change role settings", "Setting", 404, "Настройка не найдена", "Изменение настроек роли");
		}

		var permissions = await _orientDbService.GetRolePermissionsOnServerAsync(role.Id, server.Id);
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
				CanChangeRole = permissions.Contains("ServerCanChangeRole"),
				CanWorkChannels = permissions.Contains("ServerCanWorkChannels"),
				CanDeleteUsers = permissions.Contains("ServerCanDeleteUsers"),
				CanMuteOther = permissions.Contains("ServerCanMuteOther"),
				CanDeleteOthersMessages = permissions.Contains("ServerCanDeleteOthersMessages"),
				CanIgnoreMaxCount = permissions.Contains("ServerCanIgnoreMaxCount"),
				CanCreateRoles = permissions.Contains("ServerCanCreateRoles"),
				CanCreateLessons = permissions.Contains("ServerCanCreateLessons"),
				CanCheckAttendance = permissions.Contains("ServerCanCheckAttendance")
			}
		};

		var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "Updated role settings");
		}
	}
}
