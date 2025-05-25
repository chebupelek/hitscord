namespace HitscordLibrary.SocketsModels;

public class DeletedMessageInChatResponceDTO : NotificationObject
{
    public required Guid ChatId { get; set; }
    public required Guid MessageId { get; set; }
}