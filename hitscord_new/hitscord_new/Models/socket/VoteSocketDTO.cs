namespace hitscord.Models.Sockets;
public class VoteSocketDTO
{
    public required bool isChannel { get; set; }
    public required long VoteId { get; set; }
    public required Guid ChannelId { get; set; }
}