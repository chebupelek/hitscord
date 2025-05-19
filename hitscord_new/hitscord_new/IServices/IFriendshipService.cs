using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IFriendshipService
{
	Task CreateApplicationAsync(string token, Guid userId);
	Task DeleteApplicationAsync(string token, Guid applicationId);
	Task DeclineApplicationAsync(string token, Guid applicationId);
	Task ApproveApplicationAsync(string token, Guid applicationId);
	Task<ApplicationsList> GetApplicationListTo(string token);
	Task<ApplicationsList> GetApplicationListFrom(string token);
	Task<UsersList> GetFriendsListAsync(string token);
	Task DeleteFriendAsync(string token, Guid UserId);
}