using hitscord.Models.other;

namespace hitscord.Models.request;

public class ChatUserRequestDTO
{
    public required Guid ChatId { get; set; }
    public required string UserTag { get; set; }
}