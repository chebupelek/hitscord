using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Redis.Sessions.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace hitscord.Redis.Sessions;

public class RedisSessionService : IRedisSessionService
{
	private readonly IDatabase _db;

	public RedisSessionService(IConnectionMultiplexer redis)
	{
		_db = redis.GetDatabase();
	}

	private static readonly TimeSpan AccessLifetime = TimeSpan.FromDays(1);
	private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(10);

	// 1) Управление сессиями пользователей

	public async Task<string> CreateSession(Guid userId, string refreshToken)
	{
		var sessionId = Guid.NewGuid().ToString();

		var session = new UserRedisSessionDTO
		{
			UserId = userId,
			RefreshToken = refreshToken,
			CreatedAt = DateTime.UtcNow,
			ExpireAt = DateTime.UtcNow.Add(SessionLifetime)
		};

		var json = JsonSerializer.Serialize(session);

		await _db.StringSetAsync($"session:user:{sessionId}", json, SessionLifetime);

		return sessionId;
	}

	public async Task<UserRedisSessionDTO?> GetSession(string sessionId)
	{
		var json = await _db.StringGetAsync($"session:user:{sessionId}");

		if(!json.HasValue)
		{
			return null;
		}

		return (JsonSerializer.Deserialize<UserRedisSessionDTO>(json!));
	}

	public async Task DeleteSession(string sessionId)
	{
		await _db.KeyDeleteAsync($"session:user:{sessionId}");
	}

	// 2) Управление сессиями админов

	public async Task<string> CreateAdminSession(Guid adminId, string accessToken)
	{
		var sessionId = Guid.NewGuid().ToString();

		var session = new AdminRedisSessionDTO
		{
			AdminId = adminId,
			AccessToken = accessToken,
			CreatedAt = DateTime.UtcNow,
			ExpireAt = DateTime.UtcNow.Add(AccessLifetime)
		};

		var json = JsonSerializer.Serialize(session);

		await _db.StringSetAsync($"session:admin:{sessionId}", json, AccessLifetime);

		return sessionId;
	}

	public async Task<AdminRedisSessionDTO?> GetAdminSession(string sessionId)
	{
		var json = await _db.StringGetAsync($"session:admin:{sessionId}");

		if(!json.HasValue)
		{
			return null;
		}

		return (JsonSerializer.Deserialize<AdminRedisSessionDTO>(json!));
	}

	public async Task DeleteAdminSession(string sessionId)
	{
		await _db.KeyDeleteAsync($"session:admin:{sessionId}");
	}
}