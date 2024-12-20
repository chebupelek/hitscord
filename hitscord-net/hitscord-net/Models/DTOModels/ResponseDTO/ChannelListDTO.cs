namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class ChannelListDTO
{
    public required List<TextChannelResponseDTO> TextChannels { get; set; }
    public required List<VoiceChannelResponseDTO> VoiceChannels { get; set; }
    //public required List<AnnouncementChannelResponseDTO> AnnouncementChannels { get; set; }
}