using hitscord.Models.db;

namespace hitscord.IServices;

public interface IAuthenticationService
{
	Task CheckSubscriptionNotExistAsync(Guid ServerId, Guid UserId);
	Task<RoleDbModel> CheckSubscriptionExistAsync(Guid ServerId, Guid UserId);
	Task<RoleDbModel> CheckUserNotCreatorAsync(Guid ServerId, Guid UserId);
	Task<RoleDbModel> CheckUserCreatorAsync(Guid ServerId, Guid UserId);
	Task CheckUserRightsChangeRoles(Guid ServerId, Guid UserId);
	Task CheckUserRightsWorkWithChannels(Guid ServerId, Guid UserId);
	Task CheckUserRightsWorkWithSubChannels(Guid ServerId, Guid UserId, Guid SubChannelId);
	Task CheckUserRightsMuteOthers(Guid ServerId, Guid UserId);
	Task CheckUserRightsDeleteUsers(Guid ServerId, Guid UserId);
	Task CheckUserRightsJoinToVoiceChannel(Guid channelId, Guid UserId);
	Task CheckUserRightsWriteInChannel(Guid channelId, Guid UserId);
	Task CheckUserRightsSeeChannel(Guid channelId, Guid UserId);
}