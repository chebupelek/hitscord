using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;

namespace hitscord_net.IServices;

public interface IChannelService
{
    Task<ChannelDbModel> CreateChannelAsync(Guid serverId, string token, string name);
    Task<ChannelListDTO> GetChannelListAsync(Guid serverId, string token);
}