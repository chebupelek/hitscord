using hitscord.Redis.CashedDB.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace hitscord.Redis.CashedDB;

public class RedisCacheService : IRedisCacheService
{
	private readonly IDatabase _db;

	public RedisCacheService(IConnectionMultiplexer redis)
	{
		_db = redis.GetDatabase();
	}

	// 2) Список пользователей сервера

	public async Task SetServerToUserAsync(Guid serverId, Guid userId)
	{
		var key = $"server:{serverId}:users";
		await _db.SetAddAsync(key, userId.ToString());
	}

	public async Task RemoveServerToUserAsync(Guid serverId, Guid userId)
	{
		var key = $"server:{serverId}:users";
		await _db.SetRemoveAsync(key, userId.ToString());

		if (await _db.SetLengthAsync(key) == 0)
		{
			await _db.KeyDeleteAsync(key);
		}
	}

	public async Task<List<Guid>> GetUsersInServerAsync(Guid serverId)
	{
		var key = $"server:{serverId}:users";
		var members = await _db.SetMembersAsync(key);

		return members.Select(x => Guid.Parse(x!)).ToList();
	}

	public async Task UpdateServerToUserFullAsync(List<UpdateServerToUserRedisDTO> dataList)
	{
		foreach(var item in dataList)
		{
			var key = $"server:{item.ServerId}:users";

			await _db.KeyDeleteAsync(key);

			if(item.UsersId.Count > 0)
			{
				var values = item.UsersId.Select(x => (RedisValue)x.ToString()).ToArray();
				await _db.SetAddAsync(key, values);
			}
		}
	}

	// 3) Права пользователя на канале

	public async Task SetUserToChannelAsync(Guid userId, Guid channelId, UserToChannelRedisDTO data)
	{
		var key = $"user:channel:{userId}:{channelId}";
		await _db.StringSetAsync(key, JsonSerializer.Serialize(data));
	}

	public async Task RemoveUserToChannelAsync(Guid userId, Guid channelId)
	{
		var key = $"user:channel:{userId}:{channelId}";
		await _db.KeyDeleteAsync(key);
	}

	public async Task<UserToChannelRedisDTO?> GetUserToChannelAsync(Guid userId, Guid channelId)
	{
		var key = $"user:channel:{userId}:{channelId}";
		var value = await _db.StringGetAsync(key);

		if(!value.HasValue)
		{
			return null;
		}

		return JsonSerializer.Deserialize<UserToChannelRedisDTO>(value!);
	}

	public async Task UpdateUserToChannelFullAsync(List<UpdateUserToChannelRedisDTO> dataList)
	{
		var batch = _db.CreateBatch();
		var tasks = new List<Task>();

		foreach(var item in dataList)
		{
			var key = $"user:channel:{item.UserId}:{item.ChannelId}";
			tasks.Add(batch.StringSetAsync(key, JsonSerializer.Serialize(item.Data)));
		}

		batch.Execute();
		await Task.WhenAll(tasks);
	}

	// 4) Список пользователей канала

	public async Task SetChannelToUserAsync(Guid channelId, ChannelToUserRedisFullDTO data)
	{
		var key = $"channel:{channelId}:users";
		await _db.HashSetAsync(key, data.UserId.ToString(), JsonSerializer.Serialize(data.Data));
	}

	public async Task RemoveChannelToUserAsync(Guid channelId, Guid userId)
	{
		var key = $"channel:{channelId}:users";
		await _db.HashDeleteAsync(key, userId.ToString());

		if (await _db.HashLengthAsync(key) == 0)
		{
			await _db.KeyDeleteAsync(key);
		}
	}

	public async Task<List<Guid>?> GetChannelToUserListFullAsync(Guid channelId)
	{
		var key = $"channel:{channelId}:users";

		var values = await _db.HashGetAllAsync(key);

		if(values.Length == 0)
		{
			return null;
		}

		return values.Select(x => Guid.Parse(x.Name!)).ToList();
	}

	public async Task<List<Guid>?> GetChannelToUserListSortedAsync(Guid channelId, List<Guid> rolesId, List<Guid> usersId)
	{
		var key = $"channel:{channelId}:users";
		var values = await _db.HashGetAllAsync(key);

		if (values.Length == 0)
		{
			return null;
		}

		var rolesSet = rolesId.ToHashSet();
		var usersSet = usersId.ToHashSet();

		var result = new List<Guid>();

		foreach (var entry in values)
		{
			var data = JsonSerializer.Deserialize<ChannelToUserRedisItemDTO>(entry.Value!);

			if (data == null || data.ChannelNotifiable != 3)
			{
				continue;
			}

			var userId = Guid.Parse(entry.Name!);

			if (data.RoleIds.Any(r => rolesSet.Contains(r)) || usersSet.Contains(userId))
			{
				result.Add(userId);
			}
		}

		return result;
	}

	public async Task UpdateChannelToUserFullAsync(List<UpdateChannelToUserRedisDTO> dataList)
	{
		var channelIds = dataList
			.Select(x => x.ChannelId)
			.Distinct()
			.ToList();

		foreach (var channelId in channelIds)
		{
			var key = $"channel:{channelId}:users";
			await _db.KeyDeleteAsync(key);
		}

		var batch = _db.CreateBatch();
		var tasks = new List<Task>();

		foreach (var item in dataList)
		{
			var key = $"channel:{item.ChannelId}:users";
			tasks.Add(batch.HashSetAsync(
				key,
				item.Data.UserId.ToString(),
				JsonSerializer.Serialize(item.Data.Data)));
		}

		batch.Execute();
		await Task.WhenAll(tasks);
	}

	public async Task UpdateUserTagAsync(List<Guid> channelsId, Guid userId, string newTag)
	{
		foreach (var channelId in channelsId)
		{
			var key = $"channel:{channelId}:users";

			var json = await _db.HashGetAsync(key, userId.ToString());
			if (!json.HasValue)
			{
				continue;
			}

			var item = JsonSerializer.Deserialize<ChannelToUserRedisItemDTO>(json!)!;
			item.UserTag = newTag;

			await _db.HashSetAsync(key, userId.ToString(), JsonSerializer.Serialize(item));
		}
	}

	public async Task UpdateUserRolesAsync(Guid channelId, Guid userId, List<Guid>? newRoleIds = null, List<string>? newRoleTags = null)
	{
		var key = $"channel:{channelId}:users";

		var json = await _db.HashGetAsync(key, userId.ToString());
		if(!json.HasValue)
		{
			return;
		}

		var item = JsonSerializer.Deserialize<ChannelToUserRedisItemDTO>(json!)!;
		if(newRoleIds != null)
		{
			item.RoleIds = newRoleIds;
		}
		if(newRoleTags != null)
		{
			item.RoleTags = newRoleTags;
		}

		await _db.HashSetAsync(key, userId.ToString(), JsonSerializer.Serialize(item));
	}

	public async Task UpdateUserNotifiableAsync(Guid channelId, Guid userId, int update)
	{
		var key = $"channel:{channelId}:users";

		var json = await _db.HashGetAsync(key, userId.ToString());
		if(!json.HasValue)
		{
			return;
		}

		var item = JsonSerializer.Deserialize<ChannelToUserRedisItemDTO>(json!)!;
		item.ChannelNotifiable += update;

		await _db.HashSetAsync(key, userId.ToString(), JsonSerializer.Serialize(item));
	}

	public async Task UpdateUserNotifiableChannelsAsync(List<Guid> channelsId, Guid userId, int update)
	{
		foreach (var channelId in channelsId)
		{
			var key = $"channel:{channelId}:users";

			var json = await _db.HashGetAsync(key, userId.ToString());
			if (!json.HasValue)
			{
				continue;
			}

			var item = JsonSerializer.Deserialize<ChannelToUserRedisItemDTO>(json!)!;
			item.ChannelNotifiable += update;

			await _db.HashSetAsync(key, userId.ToString(), JsonSerializer.Serialize(item));
		}
	}

	public async Task UpdateRoleTagForAllUsersAsync(Guid channelId, Guid roleId, string newRoleTag)
	{
		var key = $"channel:{channelId}:users";
		var allEntries = await _db.HashGetAllAsync(key);

		foreach(var entry in allEntries)
		{
			var userId = Guid.Parse(entry.Name!);
			var item = JsonSerializer.Deserialize<ChannelToUserRedisItemDTO>(entry.Value!);

			if(item == null)
			{
				continue;
			}

			var roleIndex = item.RoleIds.FindIndex(r => r == roleId);
			if(roleIndex != -1)
			{
				item.RoleTags[roleIndex] = newRoleTag;
				await _db.HashSetAsync(key, userId.ToString(), JsonSerializer.Serialize(item));
			}
		}
	}

	public async Task RemoveRoleFromUsersInChannelAsync(Guid channelId, Guid roleId)
{
    var key = $"channel:{channelId}:users";

    var allEntries = await _db.HashGetAllAsync(key);
    if (allEntries.Length == 0)
    {
        return;
    }

    foreach (var entry in allEntries)
    {
        var userId = Guid.Parse(entry.Name!);
        var item = JsonSerializer.Deserialize<ChannelToUserRedisItemDTO>(entry.Value!);

        if (item == null)
        {
            continue;
        }

        var index = item.RoleIds.FindIndex(r => r == roleId);
        if (index == -1)
        {
            continue;
        }

        item.RoleIds.RemoveAt(index);
        item.RoleTags.RemoveAt(index);

        if (item.RoleIds.Count == 0)
        {
            await _db.HashDeleteAsync(key, userId.ToString());
        }
        else
        {
            await _db.HashSetAsync(key, userId.ToString(), JsonSerializer.Serialize(item));
        }
    }

    if (await _db.HashLengthAsync(key) == 0)
    {
        await _db.KeyDeleteAsync(key);
    }
}
}