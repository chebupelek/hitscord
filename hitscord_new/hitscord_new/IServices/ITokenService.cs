using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface ITokenService
{
	// 1) Для обычных пользователей
	Task<TokensDTO> CreateTokensAsync(UserDbModel user);
	Task InvalidateSessionAsync(string sessionId);
	Task<TokensDTO> UpdateTokensAsync(string sessionId, string refreshToken);

	// 2) Для админов
	Task<TokenAdminDTO> CreateTokensAdminAsync(AdminDbModel admin);
	Task InvalidateSessionAdminAsync(string sessionId);
	Task<bool> CheckAdminAuthAsync(string sessionId, string accessToken);
}