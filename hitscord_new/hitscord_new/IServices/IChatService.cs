using hitscord.Models.db;
using hitscord.Models.response;
using HitscordLibrary.Models;

namespace hitscord.IServices;

public interface IChatService
{
	Task CreateChatAsync(string token, string userTag); 
	Task ChangeChatNameAsync(string token, Guid chatId, string newName);
	Task<ChatListDTO> GetChatsListAsync(string token);
	Task<ChatInfoDTO> GetChatInfoAsync(string token, Guid chatId);
	Task AddUserAsync(string token, string userTag, Guid chatId);
	Task RemoveUserAsync(string token, Guid chatId);
	Task<MessageChatListResponseDTO> GetChatMessagesAsync(string token, Guid chatId, int number, int fromStart);
}