using HitscordLibrary.Models.Rabbit;

namespace HitscordLibrary.Models;

public class MessageChatListResponseDTO : ResponseObject
{
    public required List<MessageChatResponceDTO> Messages { get; set; }
    public required int NumberOfMessages { get; set; }
    public required int NumberOfStarterMessage { get; set;}
}