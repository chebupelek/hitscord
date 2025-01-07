using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;

namespace hitscord_net.IServices;

public interface IMessageService
{
    Task CreateMessageAsync(Guid channelId, string token, string text, List<Guid>? roles, List<string>? tags, Guid? ReplyToMessageId);
    Task CreateMessageWebsocketAsync(Guid channelId, Guid UserId, string text, List<Guid>? roles, List<string>? tags, Guid? ReplyToMessageId);
    Task UpdateMessageAsync(Guid messageId, string token, string text, List<Guid>? roles, List<string>? tags);
    Task UpdateMessageWebsocketAsync(Guid messageId, Guid UserId, string text, List<Guid>? roles, List<string>? tags);
    Task DeleteMessageAsync(Guid messageId, string token);
    Task DeleteMessageWebsocketAsync(Guid messageId, Guid UserId);
}
