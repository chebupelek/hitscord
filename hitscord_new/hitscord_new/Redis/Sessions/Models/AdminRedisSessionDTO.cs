namespace hitscord.Redis.Sessions.Models;

public class AdminRedisSessionDTO
{
	public required Guid AdminId { get; set; }

	public required string AccessToken { get; set; }
	public required DateTime CreatedAt { get; set; }

	public required DateTime ExpireAt { get; set; }

	public string? Ip { get; set; }

	public string? UserAgent { get; set; }
}