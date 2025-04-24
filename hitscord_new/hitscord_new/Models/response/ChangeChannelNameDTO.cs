namespace hitscord.Models.response;

public class ChangeChannelNameDTO
{
	public required Guid ServerId { get; set; }
	public required Guid ChannelId { get; set; }
	public required string Name { get; set; }
}