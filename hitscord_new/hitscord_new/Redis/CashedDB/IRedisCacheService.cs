using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Redis.CashedDB.Models;

namespace hitscord.Redis.CashedDB;

public interface IRedisCacheService
{
	// 2) Список пользователей сервера
	Task SetServerToUserAsync(Guid serverId, Guid userId);
	Task RemoveServerToUserAsync(Guid serverId, Guid userId);
	Task<List<Guid>> GetUsersInServerAsync(Guid serverId);
	Task UpdateServerToUserFullAsync(List<UpdateServerToUserRedisDTO> dataList);


	// 3) Права пользователя на канале
	Task SetUserToChannelAsync(Guid userId, Guid channelId, UserToChannelRedisDTO data);
	Task RemoveUserToChannelAsync(Guid userId, Guid channelId);
	Task<UserToChannelRedisDTO?> GetUserToChannelAsync(Guid userId, Guid channelId);
	Task UpdateUserToChannelFullAsync(List<UpdateUserToChannelRedisDTO> dataList);


	// 4) Список пользователей канала
	Task SetChannelToUserAsync(Guid channelId, ChannelToUserRedisFullDTO data);
	Task RemoveChannelToUserAsync(Guid channelId, Guid userId);
	Task<List<Guid>?> GetChannelToUserListFullAsync(Guid channelId);
	Task<List<Guid>?> GetChannelToUserListSortedAsync(Guid channelId, List<Guid> RolesId, List<Guid> UsersId);
	Task UpdateChannelToUserFullAsync(List<UpdateChannelToUserRedisDTO> dataList);
	Task UpdateUserTagAsync(List<Guid> channelsId, Guid userId, string newTag);
	Task UpdateUserRolesAsync(Guid channelId, Guid userId, List<Guid>? newRoleIds = null, List<string>? newRoleTags = null);
	Task UpdateUserNotifiableAsync(Guid channelId, Guid userId, int update);
	Task UpdateUserNotifiableChannelsAsync(List<Guid> channelsId, Guid userId, int update);
	Task UpdateRoleTagForAllUsersAsync(Guid channelId, Guid roleId, string newRoleTag);
	Task RemoveRoleFromUsersInChannelAsync(Guid channelId, Guid roleId);

}