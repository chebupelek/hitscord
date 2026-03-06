using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface ITokenService
{
	Task<TokensDTO> CreateTokens(UserDbModel user);
	Task InvalidateRefreshTokenAsync(string token);
	Task<TokensDTO> UpdateTokens(string sessionId, string refreshToken);
}