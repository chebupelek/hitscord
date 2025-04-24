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

namespace hitscord.WebSockets;

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