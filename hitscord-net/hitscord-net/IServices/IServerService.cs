using hitscord_net.Models.DBModels;

namespace hitscord_net.IServices;

public interface IServerService
{
    Task CreateServerAsync(string token, string severName);
    Task SubscribeAsync(Guid serverId, string token);
    Task ChangeUserRoleAsync(string token, Guid serverId, Guid userId, RoleEnum role);
}