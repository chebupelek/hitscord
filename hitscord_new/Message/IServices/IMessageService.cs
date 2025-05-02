using HitscordLibrary.Models.Rabbit;
using Message.Models.Response;

namespace Message.IServices;

public interface IMessageService
{
    Task CreateMessageAsync(Guid channelId, string token, string text, Guid? ReplyToMessageId);
    Task UpdateMessageAsync(Guid messageId, string token, string text);
    Task DeleteMessageAsync(Guid messageId, string token);
    Task<ResponseObject> GetChannelMessagesAsync(ChannelRequestRabbit request);


    Task CreateMessageWebsocketAsync(Guid channelId, string token, string text, Guid? ReplyToMessageId);
    Task UpdateMessageWebsocketAsync(Guid messageId, string token, string text);
    Task DeleteMessageWebsocketAsync(Guid messageId, string token);
}
