using HitscordLibrary.Models.other;

namespace HitscordLibrary.SocketsModels;

public class ExceptionNotification : NotificationObject
{
    public required int Code {  get; set; }
    public required string Message { get; set; }
    public required string Object { get; set; }
}