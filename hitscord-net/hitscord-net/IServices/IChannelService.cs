using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;

namespace hitscord_net.IServices;

public interface IChannelService
{
    Task CreateChannelAsync(Guid serverId, string token, string name, ChannelTypeEnum channelType);
    Task<bool> JoinToVoiceChannelAsync(Guid chnnelId, string token);
    Task<bool> RemoveFromVoiceChannelAsync(Guid chnnelId, string token);
    Task<bool> DeleteChannelAsync(Guid chnnelId, string token);
    Task<ChannelSettingsDTO> GetChannelSettingsAsync(Guid chnnelId, string token);
    Task<bool> AddRoleToCanReadSettingAsync(Guid chnnelId, string token, Guid roleId);
    Task<bool> RemoveRoleFromCanReadSettingAsync(Guid chnnelId, string token, Guid roleId);
    Task<bool> AddRoleToCanWriteSettingAsync(Guid chnnelId, string token, Guid roleId);
    Task<bool> RemoveRoleFromCanWriteSettingAsync(Guid chnnelId, string token, Guid roleId);

}