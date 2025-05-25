using Authzed.Api.V0;
using Azure.Core;
using EasyNetQ;
using Grpc.Gateway.ProtocGenOpenapiv2.Options;
using HitscordLibrary.Models.Messages;
using HitscordLibrary.SocketsModels;
using Message.IServices;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Message.WebSockets;

public class WebSocketHandler
{
    private readonly WebSocketsManager _webSocketManager;
	private readonly IMessageService _messageService;
	private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketHandler(IMessageService messageService, WebSocketsManager webSocketManager, ILogger<WebSocketMiddleware> logger)
	{
		_messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
        _logger = logger;
    }

    public async Task HandleAsync(Guid userId, WebSocket socket)
    {
        _webSocketManager.AddConnection(userId, socket);

        try
        {
            _logger.LogInformation("WebSocket connection established for user {UserId}", userId);
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket connection closed by user {UserId}", userId);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                }
                else
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessageAsync(userId, json);
                }
            }
            _logger.LogInformation("WebSocket connection ended for user {UserId}", userId);
        }
        finally
        {
            _webSocketManager.RemoveConnection(userId);
        }
    }

    private async Task HandleMessageAsync(Guid userId, string json)
    {
        var messageBase = System.Text.Json.JsonSerializer.Deserialize<WebSocketMessageBase>(json);

        _logger.LogInformation("Received WebSocket message: {Json}", json);
        _logger.LogInformation("Parsed Type: {type}", messageBase.Type);

        var messageBaseJson = System.Text.Json.JsonSerializer.Serialize(messageBase);
        _logger.LogInformation("Parsed WebSocket message: {MessageBaseJson}", messageBaseJson);
        switch (messageBase?.Type)
        {
            case "New message":
                var newMessage = System.Text.Json.JsonSerializer.Deserialize<NewMessageWebsocket>(json);
                _logger.LogInformation("new message", newMessage);
                if (newMessage != null)
                {
                    var newMesssageData = newMessage.Content;
                    await _messageService.CreateMessageWebsocketAsync(newMesssageData.ChannelId, newMesssageData.Token, newMesssageData.Text, newMesssageData.ReplyToMessageId, newMesssageData.NestedChannel);
				}
				break;
			case "Delete message":
				var deleteMessage = System.Text.Json.JsonSerializer.Deserialize<DeleteMessageWebsocket>(json);
				Console.WriteLine($"User {userId} sent text: {deleteMessage?.Content}");
				if (deleteMessage != null)
				{
					var deleteMesssageData = deleteMessage.Content;
					await _messageService.DeleteMessageWebsocketAsync(deleteMesssageData.MessageId, deleteMesssageData.Token);
				}
				break;
			case "Update message":
                var updateMessage = System.Text.Json.JsonSerializer.Deserialize<UpdateMessageWebsocket>(json);
                Console.WriteLine($"User {userId} sent text: {updateMessage?.Content}");
                if (updateMessage != null)
                {
                    var updateMessageData = updateMessage.Content;
					await _messageService.UpdateMessageWebsocketAsync(updateMessageData.MessageId, updateMessageData.Token, updateMessageData.Text);
				}
                break;

			case "New message chat":
				var newMessagechat = System.Text.Json.JsonSerializer.Deserialize<NewMessageWebsocket>(json);
				_logger.LogInformation("new message chat", newMessagechat);
				if (newMessagechat != null)
				{
					var newMesssageData = newMessagechat.Content;
					await _messageService.CreateMessageToChatWebsocketAsync(newMesssageData.ChannelId, newMesssageData.Token, newMesssageData.Text, newMesssageData.ReplyToMessageId);
				}
				break;
			case "Delete message chat":
				var deleteMessagechat = System.Text.Json.JsonSerializer.Deserialize<DeleteMessageWebsocket>(json);
				Console.WriteLine($"User {userId} sent text: {deleteMessagechat?.Content}");
				if (deleteMessagechat != null)
				{
					var deleteMesssageData = deleteMessagechat.Content;
					await _messageService.DeleteMessageInChatWebsocketAsync(deleteMesssageData.MessageId, deleteMesssageData.Token);
				}
				break;
			case "Update message chat":
				var updateMessagechat = System.Text.Json.JsonSerializer.Deserialize<UpdateMessageWebsocket>(json);
				Console.WriteLine($"User {userId} sent text: {updateMessagechat?.Content}");
				if (updateMessagechat != null)
				{
					var updateMessageData = updateMessagechat.Content;
					await _messageService.UpdateMessageInChatWebsocketAsync(updateMessageData.MessageId, updateMessageData.Token, updateMessageData.Text);
				}
				break;

			default:
                Console.WriteLine("Unknown message type.");
                break;
        }
    }
}

public class WebSocketMessageBase
{
    public string Type { get; set; } = default!;
}

public class NewMessageWebsocket : WebSocketMessageBase
{
    public CreateMessageSocketDTO Content { get; set; } = default!;
}

public class UpdateMessageWebsocket : WebSocketMessageBase
{
    public UpdateMessageSocketDTO Content { get; set; } = default!;
}

public class DeleteMessageWebsocket : WebSocketMessageBase
{
    public DeleteMessageSocketDTO Content { get; set; } = default!;
}