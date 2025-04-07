namespace HitscordLibrary.SocketsModels;

public class DeletedMessageResponceDTO : NotificationObject
{
    public required Guid ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required Guid MessageId { get; set; }
}