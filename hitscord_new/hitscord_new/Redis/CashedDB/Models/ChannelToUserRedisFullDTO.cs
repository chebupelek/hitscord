namespace hitscord.Redis.CashedDB.Models;

public class ChannelToUserRedisFullDTO
{
	public required Guid UserId { get; set; }
	public required ChannelToUserRedisItemDTO Data { get; set; }
}