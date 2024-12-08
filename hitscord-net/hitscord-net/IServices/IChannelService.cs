using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;

namespace hitscord_net.IServices;

public interface IChannelService
{
    Task CreateChannelAsync(Guid serverId, string token, string name, ChannelTypeEnum channelType);
    Task<ChannelListDTO> GetChannelListAsync(Guid serverId, string token);
    Task<bool> JoinToVoiceChannelAsync(Guid chnnelId, string token);
    Task<bool> RemoveFromVoiceChannelAsync(Guid chnnelId, string token);
}