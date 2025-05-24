using hitscord.Models.db;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IServerService
{
    Task<ServerDbModel> CheckServerExistAsync(Guid serverId, bool includeChannels);
    Task<ServerDbModel> GetServerFullModelAsync(Guid serverId);

    Task<ServerIdDTO> CreateServerAsync(string token, string severName);
    Task SubscribeAsync(Guid serverId, string token, string? userName);
    Task UnsubscribeAsync(Guid serverId, string token);
    Task UnsubscribeForCreatorAsync(Guid serverId, string token, Guid newCreatorId);
    Task DeleteServerAsync(Guid serverId, string token);
    Task<ServersListDTO> GetServerListAsync(string token);
    Task ChangeUserRoleAsync(string token, Guid serverId, Guid userId, Guid roleId);
    Task<ServerInfoDTO> GetServerInfoAsync(string token, Guid serverId);
    Task DeleteUserFromServerAsync(string token, Guid serverId, Guid userId);
    Task<RolesListDTO> GetServerRolesAsync(string token, Guid serverId);
    Task ChangeServerNameAsync(Guid serverId, string token, string name);
    Task ChangeUserNameAsync(Guid serverId, string token, string name);
    Task ChangeNonNotifiableServerAsync(string token, Guid serverId);
}