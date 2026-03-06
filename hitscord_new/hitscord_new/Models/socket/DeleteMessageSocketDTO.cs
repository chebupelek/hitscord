namespace hitscord.Models.Sockets;
public class DeleteMessageSocketDTO
{
    public required long MessageId { get; set; }
	public required Guid ChannelId { get; set; }
}