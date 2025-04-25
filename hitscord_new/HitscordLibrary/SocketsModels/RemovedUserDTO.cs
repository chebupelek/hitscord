namespace HitscordLibrary.SocketsModels;

public class RemovedUserDTO : NotificationObject
{
    public required Guid ServerId { get; set; }
    public required bool IsNeedRemoveFromVC { get; set; }
}