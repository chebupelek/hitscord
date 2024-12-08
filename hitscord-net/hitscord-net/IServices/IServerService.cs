using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;

namespace hitscord_net.IServices;

public interface IServerService
{
    Task CreateServerAsync(string token, string severName);
    Task SubscribeAsync(Guid serverId, string token);
    Task<ServersListDTO> GetServerListAsync(string token);
    Task ChangeUserRoleAsync(string token, Guid serverId, Guid userId, RoleEnum role);
    Task<ServerInfoDTO> GetServerInfoAsync(string token, Guid serverId);
}