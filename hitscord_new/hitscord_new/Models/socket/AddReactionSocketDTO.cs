using hitscord.Models.other;

namespace hitscord.Models.Sockets;
public class AddReactionSocketDTO
{
    public required Guid ChannelId { get; set; }
    public required long MessageId { get; set; }
	public required string ReactionCode { get; set; }
}
