using hitscord.IServices;
using hitscord.Models.other;
using hitscord.Models.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace hitscord.WebSockets;

public class WebSocketHandler
{
    private readonly WebSocketsManager _webSocketManager;
    //private readonly ILogger<WebSocketMiddleware> _logger;
	private readonly IMessageService _messageService;

	public WebSocketHandler(WebSocketsManager webSocketManager, /*ILogger<WebSocketMiddleware> logger,*/ IMessageService messageService)
    {
		_messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		//_logger = logger;
	}

    public async Task HandleAsync(Guid userId, WebSocket socket)
    {
        _webSocketManager.AddConnection(userId, socket);

        try
        {
            //_logger.LogInformation("WebSocket connection established for user {UserId}", userId);
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    //_logger.LogInformation("WebSocket connection closed by user {UserId}", userId);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                }
                else
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessageAsync(userId, json);
                }
            }
            //_logger.LogInformation("WebSocket connection ended for user {UserId}", userId);
        }
        finally
        {
            _webSocketManager.RemoveConnection(userId);
        }
    }

	private async Task HandleMessageAsync(Guid userId, string json)
	{
		var messageBase = System.Text.Json.JsonSerializer.Deserialize<WebSocketMessageBase>(json);

		//_logger.LogInformation("Received WebSocket message: {Json}", json);
		//_logger.LogInformation("Parsed Type: {type}", messageBase.Type);

		var messageBaseJson = System.Text.Json.JsonSerializer.Serialize(messageBase);
		//_logger.LogInformation("Parsed WebSocket message: {MessageBaseJson}", messageBaseJson);

		try
		{
			switch (messageBase?.Type)
			{
				case "New message":
					var newMessage = System.Text.Json.JsonSerializer.Deserialize<NewMessageWebsocket>(json);
					//_logger.LogInformation("new message", newMessage);
					if (newMessage != null)
					{
						var newMesssageData = newMessage.Content;
						await _messageService.CreateMessageWebsocketAsync(newMesssageData);
					}
					break;

				case "Delete message":
					var deleteMessage = System.Text.Json.JsonSerializer.Deserialize<DeleteMessageWebsocket>(json);
					if (deleteMessage != null)
					{
						var deleteMesssageData = deleteMessage.Content;
						await _messageService.DeleteMessageWebsocketAsync(deleteMesssageData.MessageId, deleteMesssageData.ChannelId, deleteMesssageData.Token);
					}
					break;

				case "Update message":
					var updateMessage = System.Text.Json.JsonSerializer.Deserialize<UpdateMessageWebsocket>(json);
					if (updateMessage != null)
					{
						var updateMessageData = updateMessage.Content;
						await _messageService.UpdateMessageWebsocketAsync(updateMessageData.MessageId, updateMessageData.ChannelId, updateMessageData.Token, updateMessageData.Text);
					}
					break;



				case "New message chat":
					var newMessagechat = System.Text.Json.JsonSerializer.Deserialize<NewMessageWebsocket>(json);
					//_logger.LogInformation("new message chat", newMessagechat);
					if (newMessagechat != null)
					{
						var newMesssageData = newMessagechat.Content;
						await _messageService.CreateMessageToChatWebsocketAsync(newMesssageData);
					}
					break;

				case "Delete message chat":
					var deleteMessagechat = System.Text.Json.JsonSerializer.Deserialize<DeleteMessageWebsocket>(json);
					if (deleteMessagechat != null)
					{
						var deleteMesssageData = deleteMessagechat.Content;
						await _messageService.DeleteMessageInChatWebsocketAsync(deleteMesssageData.MessageId, deleteMesssageData.ChannelId, deleteMesssageData.Token);
					}
					break;

				case "Update message chat":
					var updateMessagechat = System.Text.Json.JsonSerializer.Deserialize<UpdateMessageWebsocket>(json);
					if (updateMessagechat != null)
					{
						var updateMessageData = updateMessagechat.Content;
						await _messageService.UpdateMessageInChatWebsocketAsync(updateMessageData.MessageId, updateMessageData.ChannelId, updateMessageData.Token, updateMessageData.Text);
					}
					break;


				case "Vote":
					var vote = System.Text.Json.JsonSerializer.Deserialize<VoteVariantSocket>(json);
					if (vote != null)
					{
						var voteData = vote.Content;
						await _messageService.VoteAsync(voteData.Token, voteData.isChannel, voteData.VoteVariantId);
					}
					break;

				case "Unvote":
					var unvote = System.Text.Json.JsonSerializer.Deserialize<VoteVariantSocket>(json);
					if (unvote != null)
					{
						var unvoteData = unvote.Content;
						await _messageService.UnVoteAsync(unvoteData.Token, unvoteData.VoteVariantId);
					}
					break;

				case "Get vote":
					var voteget = System.Text.Json.JsonSerializer.Deserialize<VoteSocket>(json);
					if (voteget != null)
					{
						var votegetData = voteget.Content;
						var result = await _messageService.GetVotingAsync(votegetData.Token, votegetData.isChannel, votegetData.ChannelId, votegetData.VoteId);
						await _webSocketManager.SendMessageAsync(userId, new
						{
							MessageType = "Vote data",
							Payload = result
						});
					}
					break;

				case "See message":
					var seeMessage = System.Text.Json.JsonSerializer.Deserialize<SeeMessage>(json);
					if (seeMessage != null)
					{
						var seeMessageData = seeMessage.Content;
						await _messageService.MessageSeeAsync(seeMessageData.Token, seeMessageData.isChannel, seeMessageData.ChannelId, seeMessageData.MessageId);
					}
					break;




				default:
					break;
			}
		}
		catch (CustomException ex)
		{
			await _webSocketManager.SendMessageAsync(userId, new
			{
				Type = "Custom error",
				Error = new
				{
					Code = ex.Code,
					Object = ex.ObjectFront,
					Message = ex.MessageFront
				}
			});
		}
		catch (Exception ex)
		{
			await _webSocketManager.SendMessageAsync(userId, new
			{
				Type = "Error",
				Error = new
				{
					Code = 500,
					Message = ex.Message,
					InnerExceptionMessage = ex.InnerException != null ? ex.InnerException.Message : null
				}
			});
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

public class VoteVariantSocket : WebSocketMessageBase
{
	public VoteVariantSocketDTO Content { get; set; } = default!;
}

public class VoteSocket : WebSocketMessageBase
{
	public VoteSocketDTO Content { get; set; } = default!;
}

public class SeeMessage : WebSocketMessageBase
{
	public SeeMessageDTO Content { get; set; } = default!;
}