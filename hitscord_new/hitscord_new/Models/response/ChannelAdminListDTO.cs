namespace hitscord.Models.response;

public class ChannelAdminListDTO
{
	public required List<TextChannelAdminResponseDTO> TextChannels { get; set; }
	public required List<VoiceChannelAdminResponseDTO> VoiceChannels { get; set; }
	public required List<TextChannelAdminResponseDTO> NotificationChannels { get; set; }
	public required List<VoiceChannelAdminResponseDTO> PairVoiceChannels { get; set; }
}