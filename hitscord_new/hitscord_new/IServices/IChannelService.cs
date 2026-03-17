using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.request;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IChannelService
{
    Task<ChannelDbModel> CheckChannelExistAsync(Guid channelId);
    Task<ChannelDbModel> CheckTextChannelExistAsync(Guid channelId);
    Task<ChannelDbModel> CheckTextOrNotificationChannelExistAsync(Guid channelId);
    Task<ChannelDbModel> CheckTextOrNotificationOrSubChannelExistAsync(Guid channelId);
    Task<VoiceChannelDbModel> CheckVoiceChannelExistAsync(Guid channelId, bool joinedUsers);
	Task<PairVoiceChannelDbModel> CheckPairVoiceChannelExistAsync(Guid channelId, bool joinedUsers);
    Task<ChannelDbModel> CheckNotificationChannelExistAsync(Guid channelId);
    Task<(ChannelDbModel Channel, ChannelTypeEnum Type)> CheckTextOrNotificationOrSubChannelExistWithTypeAsync(Guid channelId);


    Task UpdateReddisFullChannelAsync();



	Task CreateChannelAsync(Guid serverId, Guid UserId, string name, ChannelTypeEnum channelType, int? maxCount);
    Task<UserVoiceChannelResponseDTO> JoinToVoiceChannelAsync(Guid chnnelId, Guid UserId);
    Task<bool> RemoveFromVoiceChannelAsync(Guid channelId, Guid UserId);
    Task<bool> RemoveUserFromVoiceChannelAsync(Guid channelId, Guid UserId, Guid RemovedUserId);
    Task<bool> ChangeSelfMuteStatusAsync(Guid UserId);
    Task<bool> ChangeUserMuteStatusAsync(Guid MutedUserId, Guid UserId);
    Task<bool> DeleteChannelAsync(Guid chnnelId, Guid UserId);
    Task<ChannelSettingsDTO> GetChannelSettings(Guid chnnelId, Guid UserId);
	Task<bool> ChangeVoiceChannelSettingsAsync(Guid UserId, ChannelRoleDTO settingsData);
	Task<bool> ChangeTextChannelSettingsAsync(Guid UserId, ChannelRoleDTO settingsData);
	Task<bool> ChangeNotificationChannelSettingsAsync(Guid UserId, ChannelRoleDTO settingsData);
	Task<bool> ChangeSubChannelSettingsAsync(Guid UserId, ChannelRoleDTO settingsData);
	Task ChangeChannnelNameAsync(Guid UserId, Guid channelId, string name);
	Task<MessageListResponseDTO> MessagesListAsync(Guid channelId, Guid UserId, int number, long fromMessageId, bool down);
    Task<bool> ChangeStreamStatusAsync(Guid UserId);
    Task<UserVoiceChannelCheck?> CheckVoiceChannelAsync(Guid UserId);
    Task ChangeNonNotifiableChannelAsync(Guid UserId, Guid channelId);
    Task ChangeVoiceChannelMaxCount(Guid UserId, Guid voiceChannelId, int maxCount);
    Task<UsersIdList> GetUserThatCanSeeChannelAsync(Guid UserId, Guid channelId);
    Task<ChannelTypeEnum> GetChannelType(Guid channelId);
    Task<MessageSubChannelResponceDTO?> GetSubChannelDataAsync(Guid UserId, Guid ChannelId, long MessageId);



	Task RemoveChannels();
}