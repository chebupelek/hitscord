namespace HitscordLibrary.SocketsModels;

public class NotificationDTO 
{
    public required NotificationObject Notification {  get; set; }
    public required List<Guid> UserIds { get; set; }
    public required string Message { get; set; }
}