using HitscordLibrary.SocketsModels;
using Sockets.IServices;
using Sockets.WebSockets;

namespace Sockets.Services;

public class WebSocketService : IWebSocketService
{

    private readonly WebSocketsManager _webSocketManager;

    public WebSocketService(WebSocketsManager webSocketManager)
    {
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
    }

    public async Task MakeAutentification(NotificationObject notification, List<Guid> userIds, string message)
    {
        if (notification is ChangeSelfMutedStatus requestChangeSelfMutedStatus)
        {
            await _webSocketManager.BroadcastMessageAsync(requestChangeSelfMutedStatus, userIds, message);
        }
        if (notification is ChangeStreamStatus requestChangeStreamStatus)
        {
            await _webSocketManager.BroadcastMessageAsync(requestChangeStreamStatus, userIds, message);
        }
        if (notification is ChannelResponseSocket requestChannelResponseSocket)
        {
            await _webSocketManager.BroadcastMessageAsync(requestChannelResponseSocket, userIds, message);
        }
        if (notification is ChannelRoleResponseSocket requestChannelRoleResponseSocket)
        {
            await _webSocketManager.BroadcastMessageAsync(requestChannelRoleResponseSocket, userIds, message);
        }
        if (notification is DeletedMessageResponceDTO requestDeletedMessageResponceDTO)
        {
            await _webSocketManager.BroadcastMessageAsync(requestDeletedMessageResponceDTO, userIds, message);
        }
        if (notification is ExceptionNotification requestExceptionNotification)
        {
            await _webSocketManager.BroadcastMessageAsync(requestExceptionNotification, userIds, message);
        }
        if (notification is MessageResponceSocket requestMessageResponceSocket)
        {
            await _webSocketManager.BroadcastMessageAsync(requestMessageResponceSocket, userIds, message);
        }
        if (notification is NewSubscribeResponseDTO requestNewSubscribeResponseDTO)
        {
            await _webSocketManager.BroadcastMessageAsync(requestNewSubscribeResponseDTO, userIds, message);
        }
        if (notification is NewUserRoleResponseDTO requestNewUserRoleResponseDTO)
        {
            await _webSocketManager.BroadcastMessageAsync(requestNewUserRoleResponseDTO, userIds, message);
        }
        if (notification is ServerDeleteDTO requestServerDeleteDTO)
        {
            await _webSocketManager.BroadcastMessageAsync(requestServerDeleteDTO, userIds, message);
        }
        if (notification is UnsubscribeResponseDTO requestUnsubscribeResponseDTO)
        {
            await _webSocketManager.BroadcastMessageAsync(requestUnsubscribeResponseDTO, userIds, message);
        }
        if (notification is UserVoiceChannelResponseDTO requestUserVoiceChannelResponseDTO)
        {
            await _webSocketManager.BroadcastMessageAsync(requestUserVoiceChannelResponseDTO, userIds, message);
        }
    }
}