using Authzed.Api.V0;
using Grpc.Gateway.ProtocGenOpenapiv2.Options;
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
        var buffer = new byte[1024 * 4];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Клиент запросил закрытие соединения.");
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                    break;
                }

                // Здесь — логика обработки сообщения
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.LogInformation("Получено сообщение от {userId}: {message}", userId, message);
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning("WebSocket разорван некорректно: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанное исключение в WebSocket");
        }
        finally
        {
            if (socket.State != WebSocketState.Closed)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                catch { /* ignore */ }
            }

            socket.Dispose();
            _logger.LogInformation("Соединение закрыто для {userId}", userId);
        }
    }


    private async Task HandleMessageAsync(Guid userId, string json)
    {
        var messageBase = JsonSerializer.Deserialize<WebSocketMessageBase>(json);
        _logger.LogInformation("Received message from user {UserId}: {Message}", userId, messageBase);
    }
}

public class WebSocketMessageBase
{
    public string Type { get; set; } = default!;
}