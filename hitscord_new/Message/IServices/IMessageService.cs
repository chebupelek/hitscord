using HitscordLibrary.Models.Rabbit;
using Message.Models.Response;

namespace Message.IServices;

public interface IMessageService
{
    Task<ResponseObject> GetChannelMessagesAsync(ChannelRequestRabbit request);
    Task CreateMessageWebsocketAsync(Guid channelId, string token, string text, Guid? ReplyToMessageId, bool NestedChannel);
    Task UpdateMessageWebsocketAsync(Guid messageId, string token, string text);
    Task DeleteMessageWebsocketAsync(Guid messageId, string token);

	Task<ResponseObject> GetChatMessagesAsync(ChannelRequestRabbit request);
    Task CreateMessageToChatWebsocketAsync(Guid chatId, string token, string text, Guid? ReplyToMessageId);
    Task UpdateMessageInChatWebsocketAsync(Guid messageId, string token, string text);
    Task DeleteMessageInChatWebsocketAsync(Guid messageId, string token);
}
