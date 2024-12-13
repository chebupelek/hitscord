using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;

namespace hitscord_net.IServices;

public interface IServerService
{
    Task CreateServerAsync(string token, string severName);
    Task SubscribeAsync(Guid serverId, string token, string? userName);
    Task UnsubscribeAsync(Guid serverId, string token);
    Task DeleteServerAsync(Guid serverId, string token);
    Task<ServersListDTO> GetServerListAsync(string token);
    Task ChangeUserRoleAsync(string token, Guid serverId, Guid userId, Guid roleId);
    Task<ServerInfoDTO> GetServerInfoAsync(string token, Guid serverId);
    Task CreateRolesAsync();
    Task<List<RoleDbModel>> GetRolesAsync();
}