namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class VoiceChannelResponseDTO
{
    public required Guid ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required bool CanJoin { get; set; }
    public required List<VoiceChannelUserDTO> Users { get; set; }
}