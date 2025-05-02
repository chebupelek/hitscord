using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Models.request;
using System.Runtime.CompilerServices;

namespace hitscord.IServices;

public interface IAuthorizationService
{
    Task<bool> CheckUserAuthAsync(string token);
    Task<UserDbModel> GetUserAsync(string token);
    Task<UserDbModel> GetUserAsync(Guid userId);
    Task<TokensDTO> CreateAccount(UserRegistrationDTO registrationData);
    Task<TokensDTO> LoginAsync(LoginDTO loginData);
    Task LogoutAsync(string token);
    Task<TokensDTO> RefreshTokensAsync(string token);
    Task<ProfileDTO> GetProfileAsync(string token);
    Task<ProfileDTO> ChangeProfileAsync(string token, ChangeProfileDTO newData);
    Task ChangeCanMessageAsync(string token);
    Task ChangeCanNotification(string token);
}