using hitscord.Models.response;
using hitscord.Models.Sockets;

namespace hitscord.IServices;

public interface IMessageService
{
	Task CreateMessageWebsocketAsync(CreateMessageSocketDTO Content, Guid UserId);
	Task UpdateMessageWebsocketAsync(long messageId, Guid channelId, Guid UserId, string text);
	Task DeleteMessageWebsocketAsync(long messageId, Guid channelId, Guid UserId);

	Task CreateMessageToChatWebsocketAsync(CreateMessageSocketDTO Content, Guid UserId);
    Task UpdateMessageInChatWebsocketAsync(long messageId, Guid chatId, Guid UserId, string text);
    Task DeleteMessageInChatWebsocketAsync(long messageId, Guid chatId, Guid UserId);

	Task VoteAsync(Guid UserId, bool channel, Guid variantId);
	Task UnVoteAsync(Guid UserId, Guid variantId);
	Task<VoteResponceDTO> GetVotingAsync(Guid UserId, bool channel, Guid channelId, long voteId);

    Task RemoveMessagesFromDBAsync();

	Task MessageSeeAsync(Guid UserId, bool channel, Guid channelId, long messageId);
}
