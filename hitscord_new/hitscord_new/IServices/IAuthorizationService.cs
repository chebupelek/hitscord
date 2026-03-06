using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Models.request;
using System.Runtime.CompilerServices;

namespace hitscord.IServices;

public interface IAuthorizationService
{
    Task<UserDbModel> GetUserAsync(Guid userId);
    Task<UserDbModel> GetUserByTagAsync(string UserTag);
	Task<TokensDTO> CreateAccount(UserRegistrationDTO registrationData);
    Task<TokensDTO> LoginAsync(LoginDTO loginData);
    Task<ProfileDTO> GetProfileAsync(Guid UserId);
    Task<ProfileDTO> ChangeProfileAsync(Guid UserId, ChangeProfileDTO newData);
    Task ChangeNotifiableAsync(Guid UserId);
    Task ChangeFriendshipAsync(Guid UserId);
    Task ChangeNonFriendAsync(Guid UserId);
    Task ChangeNotificationLifetimeAsync(Guid UserId, int time);
	Task<UserResponseDTO> GetUserDataByIdAsync(Guid SearchedUserId);
    Task<FileMetaResponseDTO> ChangeUserIconAsync(Guid UserId, IFormFile iconFile);
    Task DeleteUserIconAsync(Guid UserId);
}