using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using System.Runtime.CompilerServices;

namespace hitscord_net.IServices;

public interface IAuthService
{
    Task<bool> CheckUserAuthAsync(string token);
    Task<UserDbModel> GetUserByTokenAsync(string token);
    Task<UserDbModel> GetUserByIdAsync(Guid userId);
    Task<Guid?> GetUserIdAsync(string token);
    Task<TokensDTO> CreateAccount(UserRegistrationDTO registrationData);
    Task<TokensDTO> LoginAsync(LoginDTO loginData);
    Task LogoutAsync(string token);
    Task<TokensDTO> RefreshTokensAsync(string token);
    Task<ProfileDTO> GetProfileAsync(string token);
}