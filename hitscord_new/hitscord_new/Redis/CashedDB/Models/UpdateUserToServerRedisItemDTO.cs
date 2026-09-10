namespace hitscord.Redis.CashedDB.Models;

public class UpdateUserToServerRedisItemDTO
{
	public required Guid UserId { get; set; }
	public required Guid ServerId { get; set; }
	public required UserToServersRedisDTO Data { get; set; }
}