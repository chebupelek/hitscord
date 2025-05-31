using Grpc.Core;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.OrientDb.Service;
using HitscordLibrary.Models.other;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static Authzed.Api.V1.CaveatEvalInfo.Types;

namespace hitscord.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly HitsContext _hitsContext;
    private readonly OrientDbService _orientDbService;

    public AuthenticationService(HitsContext hitsContext, ITokenService tokenService, OrientDbService orientDbService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
    }

	public async Task CheckSubscriptionNotExistAsync(Guid serverId, Guid userId)
	{
		bool isSubscribed = await _orientDbService.IsUserSubscribedToServerAsync(userId, serverId);
		if (isSubscribed)
		{
			throw new CustomException("User is already subscribed to this server", "Check subscription is not exist", "User", 400, "Пользователь уже является участником этого сервера", "Проверка на отсутствие подписки");
		}
	}

	public async Task<RoleDbModel> CheckSubscriptionExistAsync(Guid serverId, Guid userId)
	{
		Guid? roleId = await _orientDbService.GetUserRoleOnServerAsync(userId, serverId);
		if (!roleId.HasValue)
		{
			throw new CustomException("User not subscriber of this server", "Check subscription is exist", "User", 401, "Пользователь не является участником этого сервера", "Проверка на наличие подписки");
		}
		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId.Value);
		if (role == null)
		{
			throw new CustomException("Role not found", "Check subscription is exist", "Role", 404, "Роль не найдена", "Проверка на наличие подписки");
		}
		return role;
	}

	public async Task<RoleDbModel> CheckUserNotCreatorAsync(Guid serverId, Guid userId)
	{
		var userRole = await CheckSubscriptionExistAsync(serverId, userId);
		if (userRole == null || userRole.Role == RoleEnum.Creator)
		{
			throw new CustomException("User is creator of this server", "Check user is not creator", "User", 401, "Пользователь - создатель сервера", "Проверка роли пользователя на Создатель");
		}
		return userRole;
	}

	public async Task<RoleDbModel> CheckUserCreatorAsync(Guid serverId, Guid userId)
	{
		var userRole = await CheckSubscriptionExistAsync(serverId, userId);
		if (userRole == null || userRole.Role != RoleEnum.Creator)
		{
			throw new CustomException("User is not creator of this server", "Check user is creator", "User", 401, "Пользователь не является создателем сервера", "Проверка роли пользователя на Создатель");
		}
		return userRole;
	}

	public async Task CheckUserRightsChangeRoles(Guid ServerId, Guid UserId)
	{
		await CheckSubscriptionExistAsync(ServerId, UserId);
		string result = await _orientDbService.GetUserRolePermissionsOnServerAsync(UserId, ServerId);
		var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == ServerId);
		if (server == null)
		{
			throw new CustomException("Server not exist", "Check user rights for changing roles", "Server", 404, "Сервер не найден", "Проверка на возможность менять роли");
		}
		if (!result.Contains("ServerCanChangeRole"))
		{
			throw new CustomException("User doesnt has rights to change roles", "Check user rights for changing roles", "User", 401, "Пользователь не может менять роли на этом сервере", "Проверка на возможность менять роли");
		}
	}

	public async Task CheckUserRightsWorkWithChannels(Guid ServerId, Guid UserId)
	{
		await CheckSubscriptionExistAsync(ServerId, UserId);
		string result = await _orientDbService.GetUserRolePermissionsOnServerAsync(UserId, ServerId);
		var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == ServerId);
		if (server == null)
		{
			throw new CustomException("Server not exist", "Check user rights for work with channels", "Server", 404, "Сервер не найден", "Проверка на возможность работать с каналами");
		}
		if (!result.Contains("ServerCanWorkChannels"))
		{
			throw new CustomException("User doesnt has rights to change roles", "Check user rights for work with channels", "User", 401, "Пользователь не может работать с каналами на этом сервере", "Проверка на возможность работать с каналами");
		}
	}

	public async Task CheckUserRightsWorkWithSubChannels(Guid ServerId, Guid UserId, Guid SubChannelId)
	{
		await CheckSubscriptionExistAsync(ServerId, UserId);
		string result = await _orientDbService.GetUserRolePermissionsOnServerAsync(UserId, ServerId);
		var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == ServerId);
		var author = await _orientDbService.IsUserAuthorOfSubChannel(UserId, SubChannelId);
		if (server == null)
		{
			throw new CustomException("Server not exist", "Check user rights for work with channels", "Server", 404, "Сервер не найден", "Проверка на возможность работать с каналами");
		}
		if (!result.Contains("ServerCanWorkChannels") && !author)
		{
			throw new CustomException("User doesnt has rights to change roles", "Check user rights for work with channels", "User", 401, "Пользователь не может работать с каналами на этом сервере", "Проверка на возможность работать с каналами");
		}
	}

	public async Task CheckUserRightsMuteOthers(Guid ServerId, Guid UserId)
	{
		await CheckSubscriptionExistAsync(ServerId, UserId);
		string result = await _orientDbService.GetUserRolePermissionsOnServerAsync(UserId, ServerId);
		var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == ServerId);
		if (server == null)
		{
			throw new CustomException("Server not exist", "Check user rights for mute others", "Server", 404, "Сервер не найден", "Проверка на возможность мутить");
		}
		if (!result.Contains("ServerCanMuteOther"))
		{
			throw new CustomException("User doesnt has rights to mute others", "Check user rights for mute others", "User", 401, "Пользователь не может мьютить других", "Проверка на возможность мутить");
		}
	}

	public async Task CheckUserRightsDeleteUsers(Guid ServerId, Guid UserId)
	{
		await CheckSubscriptionExistAsync(ServerId, UserId);
		string result = await _orientDbService.GetUserRolePermissionsOnServerAsync(UserId, ServerId);
		var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == ServerId);
		if (server == null)
		{
			throw new CustomException("Server not exist", "Check user rights for delete users from server", "Server", 404, "Сервер не найден", "Проверка на возможность удалять пользователей");
		}
		if (!result.Contains("ServerCanDeleteUsers"))
		{
			throw new CustomException("User doesnt has rights to change roles", "Check user rights for delete users from server", "User", 401, "Пользователь не может удалять на этом сервере", "Проверка на возможность удалять пользователей");
		}
	}

	public async Task<bool> CheckUserRightsIgnore(Guid ServerId, Guid UserId)
	{
		await CheckSubscriptionExistAsync(ServerId, UserId);
		string result = await _orientDbService.GetUserRolePermissionsOnServerAsync(UserId, ServerId);
		var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == ServerId);
		if (server == null)
		{
			throw new CustomException("Server not exist", "Check user rights for ignore max count", "Server", 404, "Сервер не найден", "Проверка на возможность игнорировать максимально количество");
		}
		if (!result.Contains("ServerCanIgnoreMaxCount"))
		{
			return false;
		}
		return true;
	}

	public async Task CheckUserRightsCreateRoles(Guid ServerId, Guid UserId)
	{
		await CheckSubscriptionExistAsync(ServerId, UserId);
		string result = await _orientDbService.GetUserRolePermissionsOnServerAsync(UserId, ServerId);
		var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == ServerId);
		if (server == null)
		{
			throw new CustomException("Server not exist", "Check user rights for create roles", "Server", 404, "Сервер не найден", "Проверка на возможность создавать роли");
		}
		if (!result.Contains("ServerCanCreateRoles"))
		{
			throw new CustomException("User doesnt has rights to create roles", "Check user rights for create roles", "User", 401, "Пользователь не может создавать роли", "Проверка на возможность создавать роли");
		}
	}

	public async Task CheckUserRightsJoinToVoiceChannel(Guid channelId, Guid UserId)
	{
		var channel = await _hitsContext.Channel.FirstOrDefaultAsync(s => s.Id == channelId);
		if (channel == null)
		{
			throw new CustomException("Channel not exist", "Check user rights for write in channel", "Channel", 404, "Канал не найден", "Проверка на возможность писать в канал");
		}
		var result = await _orientDbService.CanUserJoinToVoiceChannelAsync(UserId, channelId);
		if (!result)
		{
			throw new CustomException("Role of user cant join to this channel", "Check user rights for write in channel", "User", 401, "Пользователь не может присоединиться к/писать в этот канал", "Проверка на возможность писать в канал");
		}
	}

	public async Task CheckUserRightsWriteInChannel(Guid channelId, Guid UserId)
	{
		var channel = await _hitsContext.Channel.FirstOrDefaultAsync(s => s.Id == channelId);
		if (channel == null)
		{
			throw new CustomException("Channel not exist", "Check user rights for write in channel", "Channel", 404, "Канал не найден", "Проверка на возможность писать в канал");
		}
		var result = await _orientDbService.CanUserWriteToTextChannelAsync(UserId, channelId);
		if (!result)
		{
			throw new CustomException("Role of user cant join to this channel", "Check user rights for write in channel", "User", 401, "Пользователь не может присоединиться к/писать в этот канал", "Проверка на возможность писать в канал");
		}
	}

	public async Task CheckUserRightsSeeChannel(Guid channelId, Guid UserId)
	{
		var channel = await _hitsContext.Channel.FirstOrDefaultAsync(s => s.Id == channelId);
		if (channel == null)
		{
			throw new CustomException("Channel not exist", "Check user rights for write in channel", "Channel", 404, "Канал не найден", "Проверка на возможность видеть канал");
		}
		if (channel is TextChannelDbModel && ((TextChannelDbModel)channel).IsMessage == true)
		{
			var check = await _orientDbService.CanUserUseSubChannelAsync(UserId, channelId);
			if (!check)
			{
				throw new CustomException("Role of user cant see this channel", "Check user rights for write in channel", "User", 401, "Пользователь не может видеть этот канал", "Проверка на возможность видеть канал");
			}
		}
		else
		{
			var result = await _orientDbService.CanUserSeeChannelAsync(UserId, channelId);
			if (!result)
			{
				throw new CustomException("Role of user cant see this channel", "Check user rights for write in channel", "User", 401, "Пользователь не может видеть этот канал", "Проверка на возможность видеть канал");
			}
		}
	}
}
