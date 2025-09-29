namespace hitscord.Models.Sockets;
public class SeeMessageDTO
{
	public required string Token { get; set; }
	public required bool isChannel { get; set; }
	public required long MessageId { get; set; }
	public required Guid ChannelId { get; set; }
}