using HitscordLibrary.Models.other;

namespace HitscordLibrary.SocketsModels;

public class ChangeStreamStatus : NotificationObject
{
    public required Guid ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required Guid UserId { get; set; }
    public required bool IsStream { get; set; }
}