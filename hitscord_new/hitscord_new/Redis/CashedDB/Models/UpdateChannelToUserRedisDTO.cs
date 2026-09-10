namespace hitscord.Redis.CashedDB.Models;

public class UpdateChannelToUserRedisDTO
{
	public required Guid ChannelId { get; set; }
	public required ChannelToUserRedisFullDTO Data { get; set; }
}