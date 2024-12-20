namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class MessageListResponseDTO
{
    public required List<MessageResponceDTO> Messages { get; set; }
    public required int NumberOfMessages { get; set; }
    public required int NumberOfStarterMessage { get; set;}
}