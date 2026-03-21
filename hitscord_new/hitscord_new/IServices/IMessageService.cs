using hitscord.Models.response;
using hitscord.Models.Sockets;

namespace hitscord.IServices;

public interface IMessageService
{
	Task CreateMessageWebsocketAsync(CreateMessageSocketDTO Content, Guid UserId);
	Task UpdateMessageWebsocketAsync(long messageId, Guid channelId, Guid UserId, string text);
	Task<(object response, string responseMessage)> DeleteMessageWebsocketAsync(long messageId, Guid channelId, Guid UserId);

	Task CreateMessageToChatWebsocketAsync(CreateMessageSocketDTO Content, Guid UserId);
    Task UpdateMessageInChatWebsocketAsync(long messageId, Guid chatId, Guid UserId, string text);
    Task<object> DeleteMessageInChatWebsocketAsync(long messageId, Guid chatId, Guid UserId);

	Task VoteAsync(Guid UserId, bool channel, Guid variantId);
	Task UnVoteAsync(Guid UserId, Guid variantId);
	Task<VoteResponceDTO> GetVotingAsync(Guid UserId, bool channel, Guid channelId, long voteId);

	Task<(object response, string message)> AddReactionChannelAsync(Guid UserId, Guid ChannelId, long MessageId, string ReactionCode);
	Task<object> AddReactionChatAsync(Guid UserId, Guid ChatId, long MessageId, string ReactionCode);
	Task<(object response, string message)> RemoveReactionChannelAsync(Guid UserId, Guid ChannelId, Guid ReactionId);
	Task<object> RemoveReactionChatAsync(Guid UserId, Guid ChatId, Guid ReactionId);

	Task MessageSeeAsync(Guid UserId, bool channel, Guid channelId, long messageId);

	Task RemoveMessagesFromDBAsync();
}
