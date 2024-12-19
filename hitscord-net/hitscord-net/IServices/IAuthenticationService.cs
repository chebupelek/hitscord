using hitscord_net.Models.DBModels;

namespace hitscord_net.IServices;

public interface IAuthenticationService
{
    Task CheckSubscriptionNotExistAsync(Guid ServerId, Guid UserId);
    Task<UserServerDbModel> CheckSubscriptionExistAsync(Guid ServerId, Guid UserId);
    Task<UserServerDbModel> CheckUserNotCreatorAsync(Guid ServerId, Guid UserId);
    Task<UserServerDbModel> CheckUserCreatorAsync(Guid ServerId, Guid UserId);
    Task CheckUserRightsChangeRoles(Guid ServerId, Guid UserId);
    Task CheckUserRightsWorkWithChannels(Guid ServerId, Guid UserId);
    Task CheckUserRightsDeleteUsers(Guid ServerId, Guid UserId);
    Task CheckUserRightsWriteInChannel(Guid channelId, Guid UserId);
}