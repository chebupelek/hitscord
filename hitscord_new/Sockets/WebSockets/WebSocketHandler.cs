using Authzed.Api.V0;
using EasyNetQ;
using Grpc.Gateway.ProtocGenOpenapiv2.Options;
using HitscordLibrary.Models.Messages;
using HitscordLibrary.SocketsModels;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Sockets.WebSockets;

public class WebSocketHandler
{
    private readonly WebSocketsManager _webSocketManager;
    private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketHandler(WebSocketsManager webSocketManager, ILogger<WebSocketMiddleware> logger)
    {
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
                    using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
                    {
                        bus.PubSub.Publish(newMesssageData, "CreateMessage");
                    }
                }
                break;

            case "Delete message":
                var deleteMessage = System.Text.Json.JsonSerializer.Deserialize<DeleteMessageWebsocket>(json);
                Console.WriteLine($"User {userId} sent text: {deleteMessage?.Content}");
                if (deleteMessage != null)
                {
                    var deleteMesssageData = deleteMessage.Content;
                    using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
                    {
                        bus.PubSub.Publish(deleteMesssageData, "DeleteMessage");
                    }
                }
                break;

            case "Update message":
                var updateMessage = System.Text.Json.JsonSerializer.Deserialize<UpdateMessageWebsocket>(json);
                Console.WriteLine($"User {userId} sent text: {updateMessage?.Content}");
                if (updateMessage != null)
                {
                    var updateMessageData = updateMessage.Content;
                    using (var bus = RabbitHutch.CreateBus("host=rabbitmq"))
                    {
                        bus.PubSub.Publish(updateMessageData, "UpdateMessage");
                    }
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