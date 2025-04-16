using HitscordLibrary.Models.Rabbit;
using Message.Models.Response;

namespace Message.IServices;

public interface IMessageService
{
    Task CreateMessageAsync(Guid channelId, string token, string text, List<Guid>? roles, List<Guid>? users, Guid? ReplyToMessageId);
    Task UpdateMessageAsync(Guid messageId, string token, string text, List<Guid>? roles, List<Guid>? users);
    Task DeleteMessageAsync(Guid messageId, string token);
    Task<ResponseObject> GetChannelMessagesAsync(ChannelRequestRabbit request);


    Task CreateMessageWebsocketAsync(Guid channelId, string token, string text, List<Guid>? roles, List<Guid>? users, Guid? ReplyToMessageId);
    Task UpdateMessageWebsocketAsync(Guid messageId, string token, string text, List<Guid>? roles, List<Guid>? users);
    Task DeleteMessageWebsocketAsync(Guid messageId, string token);
}
