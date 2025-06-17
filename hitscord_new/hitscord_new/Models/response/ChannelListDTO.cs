namespace hitscord.Models.response;

public class ChannelListDTO
{
	public required List<TextChannelResponseDTO> TextChannels { get; set; }
	public required List<VoiceChannelResponseDTO> VoiceChannels { get; set; }
	public required List<NotificationChannelResponseDTO> NotificationChannels { get; set; }
	public required List<VoiceChannelResponseDTO> PairVoiceChannels { get; set; }
}