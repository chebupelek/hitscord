using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IFriendshipService
{
	Task CreateApplicationAsync(Guid UserId, string userTag);
	Task DeleteApplicationAsync(Guid UserId, Guid applicationId);
	Task DeclineApplicationAsync(Guid UserId, Guid applicationId);
	Task ApproveApplicationAsync(Guid UserId, Guid applicationId);
	Task<ApplicationsList> GetApplicationListTo(Guid UserId);
	Task<ApplicationsList> GetApplicationListFrom(Guid UserId);
	Task<UsersList> GetFriendsListAsync(Guid UserId);
	Task DeleteFriendAsync(Guid UserId, Guid DeletedFriendId);
}