namespace HitscordLibrary.Models;

public class MessageChatResponceDTO
{
    public required string MessageType { get; set; }
	public required Guid? ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required Guid Id { get; set; }
    public required Guid AuthorId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public ReplyToMessageResponceDTO? ReplyToMessage { get; set; }
}