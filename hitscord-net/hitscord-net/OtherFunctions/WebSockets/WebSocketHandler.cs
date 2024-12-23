using Authzed.Api.V0;
using Grpc.Gateway.ProtocGenOpenapiv2.Options;
using hitscord_net.IServices;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using hitscord_net.Services;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

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
        var messageBase = JsonSerializer.Deserialize<WebSocketMessageBase>(json);

        switch (messageBase?.Type)
        {
            case "New message":
                var newMessage = JsonSerializer.Deserialize<NewMessageWebsocket>(json);
                Console.WriteLine($"User {userId} sent text: {newMessage?.Content}");
                if (newMessage != null)
                {
                    var newMesssageData = newMessage.Content;
                    try
                    {
                        await _messageService.CreateNormalMessageWebsocketAsync(newMesssageData.ChannelId, userId, newMesssageData.Text, newMesssageData.Roles, newMesssageData.Tags);
                    }
                    catch (CustomException ex)
                    {
                        await _webSocketManager.BroadcastMessageAsync(new { Message = ex.Message, Type = ex.Type, Object = ex.Object, Code = ex.Code }, new List<Guid> { userId }, "New message exception");
                        Console.WriteLine($"Code: {ex.Code}; Type: {ex.Type}; Object: {ex.Object}; Message: {ex.Message};");
                    }
                    catch (Exception ex)
                    {
                        await _webSocketManager.BroadcastMessageAsync(new { Message = ex.Message, Code = 500 }, new List<Guid> { userId }, "New message exception");
                        Console.WriteLine($"Message: {ex.Message};");
                    }
                }
                break;

            case "Delete message":
                var deleteMessage = JsonSerializer.Deserialize<DeleteMessageWebsocket>(json);
                Console.WriteLine($"User {userId} sent text: {deleteMessage?.Content}");
                if (deleteMessage != null)
                {
                    var deleteMesssageData = deleteMessage.Content;
                    try
                    {
                        await _messageService.DeleteNormalMessageWebsocketAsync(deleteMesssageData, userId);
                    }
                    catch (CustomException ex)
                    {
                        await _webSocketManager.BroadcastMessageAsync(new { Message = ex.Message, Type = ex.Type, Object = ex.Object, Code = ex.Code }, new List<Guid> { userId }, "Delete message exception");
                        Console.WriteLine($"Code: {ex.Code}; Type: {ex.Type}; Object: {ex.Object}; Message: {ex.Message};");
                    }
                    catch (Exception ex)
                    {
                        await _webSocketManager.BroadcastMessageAsync(new { Message = ex.Message, Code = 500 }, new List<Guid> { userId }, "Delete message exception");
                        Console.WriteLine($"Message: {ex.Message};");
                    }
                }
                break;

            case "Update message":
                var updateMessage = JsonSerializer.Deserialize<UpdateMessageWebsocket>(json);
                Console.WriteLine($"User {userId} sent text: {updateMessage?.Content}");
                if (updateMessage != null)
                {
                    var updateMessageData = updateMessage.Content;
                    try
                    {
                        await _messageService.UpdateNormalMessageWebsocketAsync(updateMessageData.MessageId, userId, updateMessageData.Text, updateMessageData.Roles, updateMessageData.Tags);
                    }
                    catch (CustomException ex)
                    {
                        await _webSocketManager.BroadcastMessageAsync(new { Message = ex.Message, Type = ex.Type, Object = ex.Object, Code = ex.Code }, new List<Guid> { userId }, "Update message exception");
                        Console.WriteLine($"Code: {ex.Code}; Type: {ex.Type}; Object: {ex.Object}; Message: {ex.Message};");
                    }
                    catch (Exception ex)
                    {
                        await _webSocketManager.BroadcastMessageAsync(new { Message = ex.Message, Code = 500 }, new List<Guid> { userId }, "Update message exception");
                        Console.WriteLine($"Message: {ex.Message};");
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
    public CreateMessageDTO Content { get; set; } = default!;
}

public class UpdateMessageWebsocket : WebSocketMessageBase
{
    public UpdateMessageDTO Content { get; set; } = default!;
}

public class DeleteMessageWebsocket : WebSocketMessageBase
{
    public Guid Content { get; set; } = default!;
}