namespace HitscordLibrary.SocketsModels;

public class ServerDeleteDTO : NotificationObject
{
    public required Guid ServerId { get; set; }
}