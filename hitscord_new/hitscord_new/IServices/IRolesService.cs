using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IRolesService
{
	Task<RolesItemDTO> CreateRoleAsync(string token, Guid serverId, string roleName, string color);
	Task DeleteRoleAsync(string token, Guid serverId, Guid roleId);
	Task UpdateRoleAsync(string token, Guid serverId, Guid roleId, string name, string color);
	Task<RolesListDTO> GetServerRolesAsync(string token, Guid serverId);
	Task ChangeRoleSettingsAsync(string token, Guid serverId, Guid roleId, SettingsEnum setting, bool settingsData);
}