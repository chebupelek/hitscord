using HitscordLibrary.SocketsModels;
using Sockets.IServices;
using Sockets.WebSockets;
using System.Text.Json;

namespace Sockets.Services
{
    public class WebSocketService : IWebSocketService
    {
        private readonly WebSocketsManager _webSocketManager;
        private readonly ILogger<WebSocketService> _logger;

        public WebSocketService(WebSocketsManager webSocketManager, ILogger<WebSocketService> logger)
        {
            _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task MakeAutentification(NotificationObject notification, List<Guid> userIds, string message)
        {
            _logger.LogInformation("Начало обработки аутентификации для {NotificationType}", notification.GetType().Name);

            try
            {
                if (notification is ChannelResponseSocket requestChannelResponseSocket)
                {
                    _logger.LogInformation("Отправка ChannelResponseSocket сообщения пользователям: {Message}", JsonSerializer.Serialize(requestChannelResponseSocket));
                    await _webSocketManager.BroadcastMessageAsync(requestChannelResponseSocket, userIds, message);
                }
                else if (notification is DeletedMessageResponceDTO requestDeletedMessageResponceDTO)
                {
                    _logger.LogInformation("Отправка DeletedMessageResponceDTO сообщения пользователям: {Message}", JsonSerializer.Serialize(requestDeletedMessageResponceDTO));
                    await _webSocketManager.BroadcastMessageAsync(requestDeletedMessageResponceDTO, userIds, message);
                }
                else if (notification is MessageResponceSocket requestMessageResponceSocket)
                {
                    _logger.LogInformation("Отправка MessageResponceSocket сообщения пользователям: {Message}", JsonSerializer.Serialize(requestMessageResponceSocket));
                    await _webSocketManager.BroadcastMessageAsync(requestMessageResponceSocket, userIds, message);
                }
                else if (notification is NewSubscribeResponseDTO requestNewSubscribeResponseDTO)
                {
                    _logger.LogInformation("Отправка NewSubscribeResponseDTO сообщения пользователям: {Message}", JsonSerializer.Serialize(requestNewSubscribeResponseDTO));
                    await _webSocketManager.BroadcastMessageAsync(requestNewSubscribeResponseDTO, userIds, message);
                }
                else if (notification is NewUserRoleResponseDTO requestNewUserRoleResponseDTO)
                {
                    _logger.LogInformation("Отправка NewUserRoleResponseDTO сообщения пользователям: {Message}", JsonSerializer.Serialize(requestNewUserRoleResponseDTO));
                    await _webSocketManager.BroadcastMessageAsync(requestNewUserRoleResponseDTO, userIds, message);
                }
                else if (notification is ServerDeleteDTO requestServerDeleteDTO)
                {
                    _logger.LogInformation("Отправка ServerDeleteDTO сообщения пользователям: {Message}", JsonSerializer.Serialize(requestServerDeleteDTO));
                    await _webSocketManager.BroadcastMessageAsync(requestServerDeleteDTO, userIds, message);
                }
                else if (notification is UnsubscribeResponseDTO requestUnsubscribeResponseDTO)
                {
                    _logger.LogInformation("Отправка UnsubscribeResponseDTO сообщения пользователям: {Message}", JsonSerializer.Serialize(requestUnsubscribeResponseDTO));
                    await _webSocketManager.BroadcastMessageAsync(requestUnsubscribeResponseDTO, userIds, message);
                }
                else if (notification is UserVoiceChannelResponseDTO requestUserVoiceChannelResponseDTO)
                {
                    _logger.LogInformation("Отправка UserVoiceChannelResponseDTO сообщения пользователям: {Message}", JsonSerializer.Serialize(requestUserVoiceChannelResponseDTO));
                    await _webSocketManager.BroadcastMessageAsync(requestUserVoiceChannelResponseDTO, userIds, message);
                }
                else if (notification is ChangeSelfMutedStatus requestChangeSelfMutedStatus)
                {
                    _logger.LogInformation("Отправка ChangeSelfMutedStatus сообщения пользователям: {Message}", JsonSerializer.Serialize(requestChangeSelfMutedStatus));
                    await _webSocketManager.BroadcastMessageAsync(requestChangeSelfMutedStatus, userIds, message);
                }
                else if (notification is ChannelRoleResponseSocket requestChannelRoleResponseSocket)
                {
                    _logger.LogInformation("Отправка ChannelRoleResponseSocket сообщения пользователям: {Message}", JsonSerializer.Serialize(requestChannelRoleResponseSocket));
                    await _webSocketManager.BroadcastMessageAsync(requestChannelRoleResponseSocket, userIds, message);
                }
                else
                {
                    _logger.LogWarning("Неизвестный тип уведомления: {NotificationType}", notification.GetType().Name);
                }

                _logger.LogInformation("Обработка аутентификации завершена успешно");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения через WebSocket для типа уведомления {NotificationType}", notification.GetType().Name);
                throw;
            }
        }
    }
}
