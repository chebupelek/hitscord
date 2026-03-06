using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.Redis;

public interface IRedisCacheService
{
	// 1) Права пользователя на сервере
	Task SetUserServerAsync(Guid userId, UserServerRedisDTO server);
	Task<UserServerRedisDTO?> GetUserServerAsync(Guid userId, Guid serverId);

	// 2) Список пользователей сервера
	Task AddUserToServerAsync(Guid serverId, Guid userId);
	Task RemoveUserFromServerAsync(Guid serverId, Guid userId);
	Task<List<Guid>> GetUsersInServerAsync(Guid serverId);

	// 3) Права пользователя на канале
	Task SetUserChannelAsync(Guid userId, Guid channelId, UserChannelRedisDTO channel);
	Task<UserChannelRedisDTO?> GetUserChannelAsync(Guid userId, Guid channelId);

	// 4) Список пользователей канала
	Task AddUserToChannelAsync(Guid channelId, Guid userId);
	Task RemoveUserFromChannelAsync(Guid channelId, Guid userId);
	Task<List<Guid>> GetUsersInChannelAsync(Guid channelId);
}

public class UserServerRedisDTO
{
	public Guid ServerId { get; set; }
	public int ServerRights { get; set; }
	public bool ServerNotifiable { get; set; }
	public List<Guid> RoleIds { get; set; } = new();
	public List<string> RoleTags { get; set; } = new();
}

public class UserChannelRedisDTO
{
	public Guid ChannelId { get; set; }
	public List<Guid> RoleIds { get; set; } = new();
	public List<string> RoleTags { get; set; } = new();
	public int ChannelRights { get; set; }
	public bool ChannelNotifiable { get; set; }
}