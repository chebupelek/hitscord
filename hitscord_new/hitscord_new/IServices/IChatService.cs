using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IChatService
{
	Task<ChatInfoDTO> CreateChatAsync(string token, string userTag); 
	Task ChangeChatNameAsync(string token, Guid chatId, string newName);
	Task<ChatListDTO> GetChatsListAsync(string token);
	Task<ChatInfoDTO> GetChatInfoAsync(string token, Guid chatId);
	Task AddUserAsync(string token, string userTag, Guid chatId);
	Task RemoveUserAsync(string token, Guid chatId);
	Task<MessageListResponseDTO> GetChatMessagesAsync(string token, Guid chatId, int number, long fromMessageId, bool down);
	Task ChangeChatIconAsync(string token, Guid chatId, IFormFile iconFile);
	Task ChangeNonNotifiableChatAsync(string token, Guid chatId);
}