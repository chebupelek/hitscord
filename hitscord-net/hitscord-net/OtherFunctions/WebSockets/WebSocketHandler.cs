using hitscord_net.IServices;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.InnerModels;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace hitscord_net.OtherFunctions.WebSockets;

public class WebSocketHandler
{
    private readonly WebSocketsManager _webSocketManager;
    private readonly IMessageService _messageService;

    public WebSocketHandler(WebSocketsManager webSocketManager, IMessageService messageService)
    {
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
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
        // Десериализуем базовое сообщение
        WebSocketMessageBase messageBase;
        try
        {
            messageBase = JsonSerializer.Deserialize<WebSocketMessageBase>(json);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Invalid JSON format: {ex.Message}");
            await SendErrorMessageAsync(userId, "Invalid JSON format.");
            return;
        }

        switch (messageBase?.Type)
        {
            case "createMessage":
                var createMessageDto = JsonSerializer.Deserialize<CreateMessageWebSocketMessage>(json);
                if (createMessageDto != null)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(createMessageDto.Text))
                        {
                            throw new CustomException("Message text cannot be empty.", "ValidationError", "Message", 400);
                        }

                        if (createMessageDto.Text.Length > 5000)
                        {
                            throw new CustomException("Message text exceeds the maximum length.", "ValidationError", "Message", 400);
                        }

                        await _messageService.CreateNormalMessageWebsocketAsync(
                            createMessageDto.ChannelId,
                            userId,
                            createMessageDto.Text,
                            createMessageDto.Roles,
                            createMessageDto.Tags);

                        Console.WriteLine($"Message from user {userId} created for channel {createMessageDto.ChannelId}");
                    }
                    catch (CustomException ex)
                    {
                        Console.WriteLine($"Custom error while creating message: {ex.Message}");
                        await SendErrorMessageAsync(userId, $"Custom error occurred: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while creating message: {ex.Message}");
                        await SendErrorMessageAsync(userId, $"Error occurred: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid message format for createMessage");
                    await SendErrorMessageAsync(userId, "Invalid message format for createMessage");
                }
                break;

            default:
                Console.WriteLine("Unknown message type.");
                break;
        }
    }

    private async Task SendErrorMessageAsync(Guid userId, string errorMessage)
    {
        var errorMessageDto = new
        {
            Type = "error",
            Message = errorMessage
        };

        var jsonError = JsonSerializer.Serialize(errorMessageDto);
        var socket = _webSocketManager.GetConnection(userId);

        if (socket != null && socket.State == WebSocketState.Open)
        {
            var buffer = Encoding.UTF8.GetBytes(jsonError);
            await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

public class WebSocketMessageBase
{
    public string Type { get; set; } = default!;
}

public class CreateMessageWebSocketMessage : WebSocketMessageBase
{
    public Guid ChannelId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(5000)]
    public string Text { get; set; } = default!;

    public List<Guid>? Roles { get; set; }
    public List<string>? Tags { get; set; }
}

