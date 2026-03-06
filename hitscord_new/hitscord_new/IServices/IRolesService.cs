using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IRolesService
{
	Task<RolesItemDTO> CreateRoleAsync(Guid UserId, Guid serverId, string roleName, string color);
	Task DeleteRoleAsync(Guid UserId, Guid serverId, Guid roleId);
	Task UpdateRoleAsync(Guid UserId, Guid serverId, Guid roleId, string name, string color);
	Task<RolesListDTO> GetServerRolesAsync(Guid UserId, Guid serverId);
	Task ChangeRoleSettingsAsync(Guid UserId, Guid serverId, Guid roleId, SettingsEnum setting, bool settingsData);
}