using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Redis.Sessions.Models;

namespace hitscord.Redis.Sessions;

public interface IRedisSessionService
{
	// 1) Управление сессиями пользователей
	Task<string> CreateSession(Guid userId, string refreshToken);
	Task<UserRedisSessionDTO?> GetSession(string sessionId);
	Task DeleteSession(string sessionId);

	// 2) Управление сессиями админов
	Task<string> CreateAdminSession(Guid adminId, string accessToken);
	Task<AdminRedisSessionDTO?> GetAdminSession(string sessionId);
	Task DeleteAdminSession(string sessionId);
}