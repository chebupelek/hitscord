using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Models.request;
using System.Runtime.CompilerServices;
using hitscord.Models.other;

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
}