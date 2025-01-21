using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;

namespace hitscord_net.IServices;

public interface IFriendshipService
{
    Task CreateFriendshipApplicationAsync(string token, Guid userApplicationTo);
    Task DeleteFriendshipApplicationAsync(string token, Guid userApplicationTo);
    Task AccessFriendshipApplicationAsync(string token, Guid userId);
    Task<List<FriendshipApplicationDTO>> GetFriendshipApplicationsListFromMeAsync(string token);
    Task<List<FriendshipApplicationDTO>> GetFriendshipApplicationsListToMeAsync(string token);
    Task<List<FriendshipApplicationDTO>> GetFriendshipListAsync(string token);
    Task RemoveFriendShipAsync(string token, Guid userId);
}
