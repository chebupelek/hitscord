namespace hitscord.Models.response;

public class VoiceChannelAdminResponseDTO
{
    public required Guid ChannelId { get; set; }
    public required string ChannelName { get; set; }
	public required int MaxCount { get; set; }
}