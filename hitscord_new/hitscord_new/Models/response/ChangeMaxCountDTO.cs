namespace hitscord.Models.response;

public class ChangeMaxCountDTO
{
	public required Guid ServerId { get; set; }
	public required Guid VoiceChannelId { get; set; }
	public required int MaxCount { get; set; }
}