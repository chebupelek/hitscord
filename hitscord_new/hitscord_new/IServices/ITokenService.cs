using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface ITokenService
{
    TokensDTO CreateTokens(UserDbModel user);
    Task ValidateTokenAsync(string accessToken, string refreshToken, Guid? userId);
    Task InvalidateTokenAsync(string token);
    Task InvalidateRefreshTokenAsync(string token);
    Task<bool> IsTokenValidAsync(string token);
    bool IsTokenExpired(string token);
    Task<bool> CheckRefreshToken(string token);
    Task<TokensDTO> UpdateTokens(string refreshToken);
    Task BanningTokensAsync();
}