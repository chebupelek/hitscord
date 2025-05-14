using hitscord.Models.db;

namespace hitscord.Models.response;

public class UserVoiceChannelCheck
{
    public required Guid ServerId { get; set; }
	public required Guid VoiceChannelId { get; set; }
}