using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Models.request;
using System.Runtime.CompilerServices;
using hitscord.Models.other;
using hitscord_new.Models.response;

namespace hitscord.IServices;

public interface IAdminService
{
	Task CreateAccount(Guid adminId, AdminRegistrationDTO registrationData);
	Task<TokenAdminDTO> LoginAsync(AdminLoginDTO loginData);
	Task<UsersAdminListDTO> UsersListAsync(Guid adminId, int num, int page, UsersSortEnum? sort, string? name, string? mail, List<Guid>? rolesIds);
	Task<ChannelsAdminListDTO> DeletedChannelsListAsync(Guid adminId, int num, int page);
	Task RewiveDeletedChannel(Guid adminId, Guid ChannelId);
	Task<SystemRolesFullListDTO> RolesFullListAsync(Guid adminId);
	Task<SystemRolesFullListDTO> RolesShortListAsync(Guid adminId, string? name);
	Task CreateSystemRoleAsync(Guid adminId, Guid ParentRoleId, string name);
	Task RenameSystemRoleAsync(Guid adminId, Guid RoleId, string name);
	Task DeleteSystemRoleAsync(Guid adminId, Guid RoleId);
	Task AddSystemRoleAsync(Guid adminId, Guid RoleId, List<Guid> UsersIds);
	Task RemoveSystemRoleAsync(Guid adminId, Guid RoleId, Guid UserId);
	Task<AdminDbModel> CreateAccountOnce();
	Task<FileResponseDTO> GetIconAsync(Guid adminId, Guid fileId);
	Task<OperationsListDTO> GetOperationHistoryAsync(Guid adminId, int num, int page);
	Task ChangeUserPasswordAsync(Guid adminId, Guid userId, string newPassword);
	Task<ServersAdminListDTO> GetServersListAsync(Guid adminId, int num, int page, string? name);
	Task<ServerAdminInfoDTO> GetServerDataAsync(Guid adminId, Guid ServerId);

	Task AddUserAsync(Guid adminId, string Mail, string Name, string Password, IFormFile? iconFile);
	Task ChangeUserIconAdminAsync(Guid adminId, Guid userId, IFormFile iconFile);
	Task DeleteUserIconAdminAsync(Guid adminId, Guid userId);
	Task ChangeUserDataAsync(Guid adminId, Guid UserId, string? Mail, string? Name);
	Task DeleteUserAsync(Guid adminId, Guid UserId);


	Task ChangeServerDataAsync(Guid adminId, Guid serverId, string? serverName, ServerTypeEnum? serverType, bool? serverClosed, Guid? newCreatorId);
	Task ChangeServerIconAdminAsync(Guid adminId, Guid serverId, IFormFile iconFile);
	Task DeleteServerIconAdminAsync(Guid adminId, Guid serverId);
	Task<RolesItemDTO> CreateRoleAdminAsync(Guid adminId, Guid serverId, string roleName, string color);
	Task DeleteRoleAdminAsync(Guid adminId, Guid serverId, Guid roleId);
	Task UpdateRoleAsync(Guid adminId, Guid serverId, Guid roleId, string name, string color);
	Task ChangeRoleSettingsAdminAsync(Guid adminId, Guid serverId, Guid roleId, SettingsEnum setting, bool settingsData);
	Task DeleteUserFromServerAdminAsync(Guid adminId, Guid serverId, Guid userId);
	Task ChangeUserNameAdminAsync(Guid serverId, Guid adminId, Guid userId, string name);
	Task AddRoleToUserAdminAsync(Guid adminId, Guid serverId, Guid userId, Guid roleId);
	Task RemoveRoleFromUserAdminAsync(Guid adminId, Guid serverId, Guid userId, Guid roleId);
	Task CreateChannelAdminAsync(Guid serverId, Guid adminId, string name, ChannelTypeEnum channelType, int? maxCount);
	Task DeleteChannelAdminAsync(Guid chnnelId, Guid adminId);
	Task ChnageChannnelNameAdminAsync(Guid adminId, Guid channelId, string name, int? number);
	Task ChangeVoiceChannelSettingsAdminAsync(Guid adminId, ChannelRoleDTO settingsData);
	Task ChangeTextChannelSettingsAdminAsync(Guid adminId, ChannelRoleDTO settingsData);
	Task ChangeNotificationChannelSettingsAdminAsync(Guid adminId, ChannelRoleDTO settingsData);
	Task<ServerPresetItemDTO> CreatePresetAdminAsync(Guid adminId, Guid serverId, Guid serverRoleId, Guid systemRoleId);
	Task DeletePresetAdminAsync(Guid adminId, Guid serverId, Guid serverRoleId, Guid systemRoleId);
}