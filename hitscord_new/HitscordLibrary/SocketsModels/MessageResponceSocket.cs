using HitscordLibrary.Models;

namespace HitscordLibrary.SocketsModels;

public class MessageResponceSocket : NotificationObject
{
    public required Guid ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required Guid Id { get; set; }
    public required string Text { get; set; }
    public required Guid AuthorId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid? NestedChannelId { get; set; }
    public MessageResponceSocket? ReplyToMessage { get; set; }
}