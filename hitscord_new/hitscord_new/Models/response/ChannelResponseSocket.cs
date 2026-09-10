using hitscord.Models.other;

namespace hitscord.Models.response;

public class ChannelResponseSocket
{
    public required bool Create { get; set; }
    public required Guid ServerId { get; set; }
	public Guid? GroupId { get; set; }
	public required Guid ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required ChannelTypeEnum ChannelType { get; set; }
    public required int Position { get; set; }
}