using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;

namespace hitscord_net.IServices;

public interface ITokenService
{
    TokensDTO CreateTokens(UserDbModel user);
    string CreateApplicationToken(RegistrationApplicationDbModel application);
    Task ValidateTokenAsync(string accessToken, string refreshToken, Guid? userId);
    Task InvalidateTokenAsync(string token);
    Task<bool> IsTokenValidAsync(string token);
    bool IsTokenExpired(string token);
    Task<bool> CheckRefreshToken(string token);
    Task<TokensDTO> UpdateTokens(Guid user, string token);
    Task BanningTokensAsync();
}