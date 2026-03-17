namespace hitscord.Redis.Sessions.Models;

public class UserRedisSessionDTO
{
	public required Guid UserId { get; set; }

	public required string RefreshToken { get; set; }

	public required DateTime CreatedAt { get; set; }

	public required DateTime ExpireAt { get; set; }

	public string? Ip { get; set; }

	public string? UserAgent { get; set; }
}