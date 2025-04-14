using HitscordLibrary.SocketsModels;
using Sockets.IServices;
using Sockets.WebSockets;
using System.Text.Json;

namespace Sockets.Services
{
    public class WebSocketService : IWebSocketService
    {
        private readonly WebSocketsManager _webSocketManager;

        public WebSocketService(WebSocketsManager webSocketManager)
        {
            _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
        }

        public async Task MakeAutentification(NotificationObject notification, List<Guid> userIds, string message)
        {
            try
            {
                if (notification is ChannelResponseSocket requestChannelResponseSocket)
                {
                    await _webSocketManager.BroadcastMessageAsync(requestChannelResponseSocket, userIds, message);
                }
                else if (notification is DeletedMessageResponceDTO requestDeletedMessageResponceDTO)
                {
                    await _webSocketManager.BroadcastMessageAsync(requestDeletedMessageResponceDTO, userIds, message);
                }
                else if (notification is MessageResponceSocket requestMessageResponceSocket)
                {
                    await _webSocketManager.BroadcastMessageAsync(requestMessageResponceSocket, userIds, message);
                }
                else if (notification is NewSubscribeResponseDTO requestNewSubscribeResponseDTO)
                {
                    await _webSocketManager.BroadcastMessageAsync(requestNewSubscribeResponseDTO, userIds, message);
                }
                else if (notification is NewUserRoleResponseDTO requestNewUserRoleResponseDTO)
                {
                    await _webSocketManager.BroadcastMessageAsync(requestNewUserRoleResponseDTO, userIds, message);
                }
                else if (notification is ServerDeleteDTO requestServerDeleteDTO)
                {
                    await _webSocketManager.BroadcastMessageAsync(requestServerDeleteDTO, userIds, message);
                }
                else if (notification is UnsubscribeResponseDTO requestUnsubscribeResponseDTO)
                {
                    await _webSocketManager.BroadcastMessageAsync(requestUnsubscribeResponseDTO, userIds, message);
                }
                else if (notification is UserVoiceChannelResponseDTO requestUserVoiceChannelResponseDTO)
                {
                    await _webSocketManager.BroadcastMessageAsync(requestUserVoiceChannelResponseDTO, userIds, message);
                }
                else if (notification is ChangeSelfMutedStatus requestChangeSelfMutedStatus)
                {
                    await _webSocketManager.BroadcastMessageAsync(requestChangeSelfMutedStatus, userIds, message);
                }
                else if (notification is ChannelRoleResponseSocket requestChannelRoleResponseSocket)
                {
                    await _webSocketManager.BroadcastMessageAsync(requestChannelRoleResponseSocket, userIds, message);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
