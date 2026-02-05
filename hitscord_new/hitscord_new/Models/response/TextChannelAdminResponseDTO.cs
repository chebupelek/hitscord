namespace hitscord.Models.response;

public class TextChannelAdminResponseDTO
{
	public required Guid ChannelId { get; set; }
	public required string ChannelName { get; set; }
	public required int MessagesNumber { get; set; }
}