using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace hitscord_net.OtherFunctions.WebSockets;

public class WebSocketHandler
{
    private readonly WebSocketsManager _webSocketManager;

    public WebSocketHandler(WebSocketsManager webSocketManager)
    {
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
    }

    public async Task HandleAsync(Guid userId, WebSocket socket)
    {
        _webSocketManager.AddConnection(userId, socket);

        try
        {
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                }
                else
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessageAsync(userId, json);
                }
            }
        }
        finally
        {
            _webSocketManager.RemoveConnection(userId);
        }
    }

    private async Task HandleMessageAsync(Guid userId, string json)
    {
        var messageBase = JsonSerializer.Deserialize<WebSocketMessageBase>(json);

        switch (messageBase?.Type)
        {
            case "text":
                var textMessage = JsonSerializer.Deserialize<TextMessage>(json);
                Console.WriteLine($"User {userId} sent text: {textMessage?.Content}");
                break;

            case "notification":
                var notification = JsonSerializer.Deserialize<NotificationMessage>(json);
                Console.WriteLine($"User {userId} received notification: {notification?.Message}");
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

public class TextMessage : WebSocketMessageBase
{
    public string Content { get; set; } = default!;
}

public class NotificationMessage : WebSocketMessageBase
{
    public string Message { get; set; } = default!;
}

