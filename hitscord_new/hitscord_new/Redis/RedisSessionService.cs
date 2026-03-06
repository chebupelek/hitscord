using hitscord.Models.db;
using hitscord.Models.response;
using StackExchange.Redis;
using System.Text.Json;

namespace hitscord.Redis;

public class RedisSessionService : ISessionService
{
	private readonly IDatabase _db;

	public RedisSessionService(IConnectionMultiplexer redis)
	{
		_db = redis.GetDatabase();
	}

	public async Task<string> CreateSession(Guid userId, string refreshToken)
	{
		var sessionId = Guid.NewGuid().ToString();

		var session = new RedisSession
		{
			UserId = userId,
			RefreshToken = refreshToken,
			CreatedAt = DateTime.UtcNow,
			ExpireAt = DateTime.UtcNow.AddDays(10)
		};

		var json = JsonSerializer.Serialize(session);

		await _db.StringSetAsync(
			$"session:{sessionId}",
			json,
			TimeSpan.FromDays(10)
		);

		return sessionId;
	}

	public async Task<RedisSession?> GetSession(string sessionId)
	{
		var json = await _db.StringGetAsync($"session:{sessionId}");

		if (!json.HasValue)
			return null;

		return JsonSerializer.Deserialize<RedisSession>(json);
	}

	public async Task DeleteSession(string sessionId)
	{
		await _db.KeyDeleteAsync($"session:{sessionId}");
	}
}