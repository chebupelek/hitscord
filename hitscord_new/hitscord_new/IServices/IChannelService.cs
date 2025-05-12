using hitscord.Models.db;
using hitscord.Models.request;
using hitscord.Models.response;
using HitscordLibrary.Models;
using HitscordLibrary.Models.other;
using HitscordLibrary.Models.Rabbit;

namespace hitscord.IServices;

public interface IChannelService
{
    Task<ChannelDbModel> CheckChannelExistAsync(Guid channelId);
    Task<ChannelDbModel> CheckTextChannelExistAsync(Guid channelId);
    Task CreateChannelAsync(Guid serverId, string token, string name, ChannelTypeEnum channelType);
    Task<bool> JoinToVoiceChannelAsync(Guid chnnelId, string token);
    Task<bool> RemoveFromVoiceChannelAsync(Guid chnnelId, string token);
    Task<bool> RemoveUserFromVoiceChannelAsync(Guid chnnelId, string token, Guid UserId);
    Task<bool> ChangeSelfMuteStatusAsync(string token);
    Task<bool> ChangeUserMuteStatusAsync(string token, Guid UserId);
    Task<bool> DeleteChannelAsync(Guid chnnelId, string token);
	Task<ChannelSettingsDTO> GetVoiceChannelSettingsAsync(Guid chnnelId, string token);
	Task<ChannelSettingsDTO> GetTextChannelSettingsAsync(Guid chnnelId, string token);
	Task<ChannelSettingsDTO> GetNotificationChannelSettingsAsync(Guid chnnelId, string token);
	Task<ChannelSettingsDTO> GetSubChannelSettingsAsync(Guid chnnelId, string token);
	Task<bool> ChangeVoiceChannelSettingsAsync(string token, ChannelRoleDTO settingsData);
	Task<bool> ChangeTextChannelSettingsAsync(string token, ChannelRoleDTO settingsData);
	Task<bool> ChangeNotificationChannelSettingsAsync(string token, ChannelRoleDTO settingsData);
	Task<bool> ChangeSubChannelSettingsAsync(string token, ChannelRoleDTO settingsData);
	Task ChnageChannnelNameAsync(string jwtToken, Guid channelId, string name);
    Task<MessageListResponseDTO> MessagesListAsync(Guid channelId, string token, int number, int fromStart);
    Task<bool> ChangeStreamStatusAsync(string token);
    Task<UserVoiceChannelCheck?> CheckVoiceChannelAsync(string token);
    Task<SubChannelResponseRabbit> CreateSubChannelAsync(string token, Guid textChannelId);
    Task DeleteSubChannelAsync(string token, Guid subChannelId);
}