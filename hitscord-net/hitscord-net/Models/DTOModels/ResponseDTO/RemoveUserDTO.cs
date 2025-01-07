namespace hitscord_net.Models.DTOModels.RequestsDTO;

public class RemoveUserDTO
{
    public required Guid UserID { get; set; }
    public required Guid VoiceChannelId { get; set; }
}
