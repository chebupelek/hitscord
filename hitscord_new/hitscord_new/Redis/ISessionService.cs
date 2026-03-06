using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.Redis;

public interface ISessionService
{
	Task<string> CreateSession(Guid userId, string refreshToken);
	Task<RedisSession?> GetSession(string sessionId);
	Task DeleteSession(string sessionId);
}

public class RedisSession
{
	public Guid UserId { get; set; }

	public string RefreshToken { get; set; }

	public DateTime CreatedAt { get; set; }

	public DateTime ExpireAt { get; set; }

	public string? Ip { get; set; }

	public string? UserAgent { get; set; }
}