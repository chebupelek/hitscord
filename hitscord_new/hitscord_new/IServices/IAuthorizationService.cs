using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Models.request;
using System.Runtime.CompilerServices;
using HitscordLibrary.Models;

namespace hitscord.IServices;

public interface IAuthorizationService
{
    Task<bool> CheckUserAuthAsync(string token);
    Task<UserDbModel> GetUserAsync(string token);
    Task<UserDbModel> GetUserAsync(Guid userId);
    Task<UserDbModel> GetUserByTagAsync(string UserTag);
	Task<TokensDTO> CreateAccount(UserRegistrationDTO registrationData);
    Task<TokensDTO> LoginAsync(LoginDTO loginData);
    Task LogoutAsync(string token);
    Task<TokensDTO> RefreshTokensAsync(string token);
    Task<ProfileDTO> GetProfileAsync(string token);
    Task<ProfileDTO> ChangeProfileAsync(string token, ChangeProfileDTO newData);
    Task ChangeNotifiableAsync(string token);
    Task ChangeFriendshipAsync(string token);
    Task ChangeNonFriendAsync(string token);
    Task<UserResponseDTO> GetUserDataByIdAsync(string token, Guid userId);
    Task<FileResponseDTO> ChangeUserIconAsync(string token, IFormFile iconFile);
}