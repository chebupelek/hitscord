namespace hitscord.Redis.CashedDB.Models;

public class UpdateUserToChannelRedisDTO
{
	public required Guid UserId { get; set; }
	public required Guid ChannelId { get; set; }
	public required UserToChannelRedisDTO Data { get; set; }
}