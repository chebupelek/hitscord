using HitscordLibrary.SocketsModels;

namespace Sockets.IServices;

public interface IWebSocketService
{
    Task MakeAutentification(NotificationObject notification, List<Guid> userIds, string message);
}
