using HitscordLibrary.Models.Rabbit;

namespace HitscordLibrary.Models;

public class MessageListResponseDTO : ResponseObject
{
    public required List<MessageResponceDTO> Messages { get; set; }
    public required int NumberOfMessages { get; set; }
    public required int NumberOfStarterMessage { get; set;}
}