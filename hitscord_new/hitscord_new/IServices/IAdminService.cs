using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Models.request;
using System.Runtime.CompilerServices;
using hitscord.Models.other;
using hitscord_new.Models.response;

namespace hitscord.IServices;

public interface IAdminService
{
	Task CreateAccount(string token, AdminRegistrationDTO registrationData);
	Task<TokenDTO> LoginAsync(AdminLoginDTO loginData);
	Task LogoutAsync(string token);
	Task<UsersAdminListDTO> UsersListAsync(string token, int num, int page, UsersSortEnum? sort, string? name, string? mail, List<Guid>? rolesIds);
	Task<ChannelsAdminListDTO> DeletedChannelsListAsync(string token, int num, int page);
	Task RewiveDeletedChannel(string token, Guid ChannelId);
	Task<SystemRolesFullListDTO> RolesFullListAsync(string token);
	Task<SystemRolesFullListDTO> RolesShortListAsync(string token, string? name);
	Task CreateSystemRoleAsync(string token, Guid ParentRoleId, string name);
	Task RenameSystemRoleAsync(string token, Guid RoleId, string name);
	Task DeleteSystemRoleAsync(string token, Guid RoleId);
	Task AddSystemRoleAsync(string token, Guid RoleId, List<Guid> UsersIds);
	Task RemoveSystemRoleAsync(string token, Guid RoleId, Guid UserId);
	Task<AdminDbModel> CreateAccountOnce();
	Task<FileResponseDTO> GetIconAsync(string token, Guid fileId);
	Task<OperationsListDTO> GetOperationHistoryAsync(string token, int num, int page);
	Task ChangeUserPasswordAsync(string token, Guid userId, string newPassword);
	Task<ServersAdminListDTO> GetServersListAsync(string token, int num, int page, string? name);
	Task<ServerAdminInfoDTO> GetServerDataAsync(string token, Guid ServerId);

	Task AddUserAsync(string token, string Mail, string Name, string Password, IFormFile? iconFile);
	Task ChangeUserIconAdminAsync(string token, Guid userId, IFormFile iconFile);
	Task DeleteUserIconAdminAsync(string token, Guid userId);
	Task ChangeUserDataAsync(string token, Guid UserId, string? Mail, string? Name);
	Task DeleteUserAsync(string token, Guid UserId);


	Task ChangeServerDataAsync(string token, Guid serverId, string? serverName, ServerTypeEnum? serverType, bool? serverClosed, Guid? newCreatorId);
	Task ChangeServerIconAdminAsync(string token, Guid serverId, IFormFile iconFile);
	Task DeleteServerIconAdminAsync(string token, Guid serverId);
	Task<RolesItemDTO> CreateRoleAdminAsync(string token, Guid serverId, string roleName, string color);
	Task DeleteRoleAdminAsync(string token, Guid serverId, Guid roleId);
	Task UpdateRoleAsync(string token, Guid serverId, Guid roleId, string name, string color);
	Task ChangeRoleSettingsAdminAsync(string token, Guid serverId, Guid roleId, SettingsEnum setting, bool settingsData);
	Task DeleteUserFromServerAdminAsync(string token, Guid serverId, Guid userId);
	Task ChangeUserNameAdminAsync(Guid serverId, string token, Guid userId, string name);
	Task AddRoleToUserAdminAsync(string token, Guid serverId, Guid userId, Guid roleId);
	Task RemoveRoleFromUserAdminAsync(string token, Guid serverId, Guid userId, Guid roleId);
	Task CreateChannelAdminAsync(Guid serverId, string token, string name, ChannelTypeEnum channelType, int? maxCount);
	Task DeleteChannelAdminAsync(Guid chnnelId, string token);
	Task ChnageChannnelNameAdminAsync(string token, Guid channelId, string name, int? number);
	Task ChangeVoiceChannelSettingsAdminAsync(string token, ChannelRoleDTO settingsData);
	Task ChangeTextChannelSettingsAdminAsync(string token, ChannelRoleDTO settingsData);
	Task ChangeNotificationChannelSettingsAdminAsync(string token, ChannelRoleDTO settingsData);
	Task<ServerPresetItemDTO> CreatePresetAdminAsync(string token, Guid serverId, Guid serverRoleId, Guid systemRoleId);
	Task DeletePresetAdminAsync(string token, Guid serverId, Guid serverRoleId, Guid systemRoleId);
}