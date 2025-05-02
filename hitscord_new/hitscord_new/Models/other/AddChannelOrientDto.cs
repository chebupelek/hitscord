using HitscordLibrary.Models.other;

namespace hitscord_new.Models.other;

public class AddChannelOrientDto
{
	public required Guid ChannelId { get; set; }
	public required ChannelTypeEnum ChannelType { get; set; }
}