namespace hitscord.Models.response;

public class DeletedMessageInChatResponceDTO
{
    public required Guid ChatId { get; set; }
    public required long MessageId { get; set; }
}