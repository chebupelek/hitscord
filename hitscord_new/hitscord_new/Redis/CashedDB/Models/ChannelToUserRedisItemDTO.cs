namespace hitscord.Redis.CashedDB.Models;

public class ChannelToUserRedisItemDTO
{
	public required string UserTag { get; set; }
	public required List<Guid> RoleIds { get; set; } = new();
	public required List<string> RoleTags { get; set; } = new();
	public required int ChannelNotifiable { get; set; } = 3;
}