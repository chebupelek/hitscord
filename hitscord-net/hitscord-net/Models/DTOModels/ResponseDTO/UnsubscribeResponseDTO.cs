namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class UnsubscribeResponseDTO
{
    public required Guid UserId { get; set; }
    public required Guid ServerId { get; set; }
}