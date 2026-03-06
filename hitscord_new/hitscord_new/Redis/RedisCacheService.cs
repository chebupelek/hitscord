using hitscord.Models.db;
using hitscord.Models.response;
using StackExchange.Redis;
using StackExchange.Redis;
using System.Text.Json;

namespace hitscord.Redis;

public class RedisCacheService : IRedisCacheService
{
	private readonly IDatabase _db;

	public RedisCacheService(IConnectionMultiplexer redis)
	{
		_db = redis.GetDatabase();
	}

	// ---------------- 1) Права пользователя на сервере ----------------
	public async Task SetUserServerAsync(Guid userId, UserServerRedisDTO server)
	{
		var key = $"userserver:{userId}:{server.ServerId}";
		await _db.StringSetAsync(key, JsonSerializer.Serialize(server));
	}

	public async Task<UserServerRedisDTO?> GetUserServerAsync(Guid userId, Guid serverId)
	{
		var key = $"userserver:{userId}:{serverId}";
		var json = await _db.StringGetAsync(key);
		return json.HasValue ? JsonSerializer.Deserialize<UserServerRedisDTO>(json!) : null;
	}

	// ---------------- 2) Список пользователей сервера ----------------
	public async Task AddUserToServerAsync(Guid serverId, Guid userId)
	{
		var key = $"server:{serverId}:users";
		await _db.SetAddAsync(key, userId.ToString());
	}

	public async Task RemoveUserFromServerAsync(Guid serverId, Guid userId)
	{
		var key = $"server:{serverId}:users";
		await _db.SetRemoveAsync(key, userId.ToString());
	}

	public async Task<List<Guid>> GetUsersInServerAsync(Guid serverId)
	{
		var key = $"server:{serverId}:users";
		var members = await _db.SetMembersAsync(key);
		return members.Select(x => Guid.Parse(x)).ToList();
	}

	// ---------------- 3) Права пользователя на канале ----------------
	public async Task SetUserChannelAsync(Guid userId, Guid channelId, UserChannelRedisDTO channel)
	{
		var key = $"userchannel:{userId}:{channelId}";
		await _db.StringSetAsync(key, JsonSerializer.Serialize(channel));
	}

	public async Task<UserChannelRedisDTO?> GetUserChannelAsync(Guid userId, Guid channelId)
	{
		var key = $"userchannel:{userId}:{channelId}";
		var json = await _db.StringGetAsync(key);
		return json.HasValue ? JsonSerializer.Deserialize<UserChannelRedisDTO>(json!) : null;
	}

	// ---------------- 4) Список пользователей канала ----------------
	public async Task AddUserToChannelAsync(Guid channelId, Guid userId)
	{
		var key = $"channel:{channelId}:users";
		await _db.SetAddAsync(key, userId.ToString());
	}

	public async Task RemoveUserFromChannelAsync(Guid channelId, Guid userId)
	{
		var key = $"channel:{channelId}:users";
		await _db.SetRemoveAsync(key, userId.ToString());
	}

	public async Task<List<Guid>> GetUsersInChannelAsync(Guid channelId)
	{
		var key = $"channel:{channelId}:users";
		var members = await _db.SetMembersAsync(key);
		return members.Select(x => Guid.Parse(x)).ToList();
	}
}