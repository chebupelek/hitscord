using hitscord.Models.db;
using hitscord.Models.request;
using hitscord.Models.response;
using HitscordLibrary.Models;
using HitscordLibrary.Models.other;

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
    Task<ChannelSettingsDTO> GetChannelSettingsAsync(Guid chnnelId, string token);
    Task<bool> ChangeChannelSettingsAsync(string token, ChannelRoleDTO settingsData);
    Task ChnageChannnelNameAsync(string jwtToken, Guid channelId, string name);
    Task<bool> AddRoleToCanReadSettingAsync(Guid chnnelId, string token, Guid roleId, ChangeRoleTypeEnum type, bool add);
    Task<bool> RemoveRoleFromCanReadSettingAsync(Guid chnnelId, string token, Guid roleId, ChangeRoleTypeEnum type, bool add);
    Task<bool> AddRoleToCanWriteSettingAsync(Guid chnnelId, string token, Guid roleId, ChangeRoleTypeEnum type, bool add);
    Task<bool> RemoveRoleFromCanWriteSettingAsync(Guid chnnelId, string token, Guid roleId, ChangeRoleTypeEnum type, bool add);
    Task<MessageListResponseDTO> MessagesListAsync(Guid channelId, string token, int number, int fromStart);
    Task<bool> ChangeStreamStatusAsync(string token);
    Task<UserVoiceChannelCheck?> CheckVoiceChannelAsync(string token);
}