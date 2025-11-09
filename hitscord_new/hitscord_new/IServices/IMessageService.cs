using hitscord.Models.response;
using hitscord.Models.Sockets;

namespace hitscord.IServices;

public interface IMessageService
{
	Task CreateMessageWebsocketAsync(CreateMessageSocketDTO Content);
	Task UpdateMessageWebsocketAsync(long messageId, Guid channelId, string token, string text);
	Task DeleteMessageWebsocketAsync(long messageId, Guid channelId, string token);

	Task CreateMessageToChatWebsocketAsync(CreateMessageSocketDTO Content);
    Task UpdateMessageInChatWebsocketAsync(long messageId, Guid chatId, string token, string text);
    Task DeleteMessageInChatWebsocketAsync(long messageId, Guid chatId, string token);

	Task VoteAsync(string token, bool channel, Guid variantId);
	Task UnVoteAsync(string token, Guid variantId);
	Task<VoteResponceDTO> GetVotingAsync(string token, bool channel, Guid channelId, long voteId);

    Task RemoveMessagesFromDBAsync();

	Task MessageSeeAsync(string token, bool channel, Guid channelId, long messageId);
}
