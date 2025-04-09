namespace HitscordLibrary.SocketsModels;

public class UnsubscribeResponseDTO : NotificationObject
{
    public required Guid UserId { get; set; }
    public required Guid ServerId { get; set; }
}