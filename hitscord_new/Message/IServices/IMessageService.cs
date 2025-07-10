using HitscordLibrary.Models;
using HitscordLibrary.Models.Messages;
using HitscordLibrary.Models.Rabbit;
using Message.Models.Response;

namespace Message.IServices;

public interface IMessageService
{
	Task<ResponseObject> GetChannelMessagesAsync(ChannelRequestRabbit request);
	Task CreateMessageWebsocketAsync(CreateMessageSocketDTO Content);
	Task UpdateMessageWebsocketAsync(Guid messageId, string token, string text);
	Task DeleteMessageWebsocketAsync(Guid messageId, string token);

	Task<ResponseObject> GetChatMessagesAsync(ChannelRequestRabbit request);
	Task CreateMessageToChatWebsocketAsync(CreateMessageSocketDTO Content);
    Task UpdateMessageInChatWebsocketAsync(Guid messageId, string token, string text);
    Task DeleteMessageInChatWebsocketAsync(Guid messageId, string token);

	Task<VoteResponceDTO> VoteAsync(string token, Guid variantId);
	Task<VoteResponceDTO> UnVoteAsync(string token, Guid variantId);
	Task<VoteResponceDTO> GetVotingAsync(string token, Guid voteId);

	Task<ResponseObject> DeleteMessagesListAsync(Guid channelId);
    Task RemoveMessagesFromDBAsync();
}
