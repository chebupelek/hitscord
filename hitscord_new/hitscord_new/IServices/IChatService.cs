using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IChatService
{
	Task<ChatInfoDTO> CreateChatAsync(Guid OwnerId, string userTag); 
	Task ChangeChatNameAsync(Guid UserId, Guid chatId, string newName);
	Task<ChatListDTO> GetChatsListAsync(Guid UserId);
	Task<ChatInfoDTO> GetChatInfoAsync(Guid UserId, Guid chatId);
	Task AddUserAsync(Guid OwnerId, string userTag, Guid chatId);
	Task RemoveUserAsync(Guid UserId, Guid chatId);
	Task<MessageListResponseDTO> GetChatMessagesAsync(Guid UserId, Guid chatId, int number, long fromMessageId, bool down);
	Task ChangeChatIconAsync(Guid UserId, Guid chatId, IFormFile iconFile);
	Task DeleteChatIconAsync(Guid UserId, Guid chatId);
	Task ChangeNonNotifiableChatAsync(Guid UserId, Guid chatId);
}