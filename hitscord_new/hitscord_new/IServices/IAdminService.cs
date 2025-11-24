using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Models.request;
using System.Runtime.CompilerServices;
using hitscord.Models.other;

namespace hitscord.IServices;

public interface IAdminService
{
	Task CreateAccount(AdminRegistrationDTO registrationData);
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
}