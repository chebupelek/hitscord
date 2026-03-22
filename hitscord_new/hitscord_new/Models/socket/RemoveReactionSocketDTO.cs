using hitscord.Models.other;

namespace hitscord.Models.Sockets;
public class RemoveReactionSocketDTO
{
	public required string Token { get; set; }
	public required Guid ChannelId { get; set; }
	public required Guid ReactionId { get; set; }
}
