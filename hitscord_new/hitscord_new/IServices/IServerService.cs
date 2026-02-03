using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IServerService
{
    Task<ServerDbModel> CheckServerExistAsync(Guid serverId, bool includeChannels);
    Task<ServerDbModel> GetServerFullModelAsync(Guid serverId);

    Task<ServerIdDTO> CreateServerAsync(string token, string severName, ServerTypeEnum? type);
	Task SubscribeAsync(string token, string invitationToken, string? userName);
    Task UnsubscribeAsync(Guid serverId, string token);
    Task UnsubscribeForCreatorAsync(Guid serverId, string token, Guid newCreatorId);
    Task DeleteServerAsync(Guid serverId, string token);
    Task<ServersListDTO> GetServerListAsync(string token);
    Task AddRoleToUserAsync(string token, Guid serverId, Guid userId, Guid roleId);
	Task RemoveRoleFromUserAsync(string token, Guid serverId, Guid userId, Guid roleId);
	Task<ServerInfoDTO> GetServerInfoAsync(string token, Guid serverId);
    Task DeleteUserFromServerAsync(string token, Guid serverId, Guid userId, string? banReason);
    Task ChangeServerNameAsync(Guid serverId, string token, string name);
    Task ChangeUserNameAsync(Guid serverId, string token, string name);
    Task ChangeNonNotifiableServerAsync(string token, Guid serverId);
    Task<BanListDTO> GetBannedListAsync(string token, Guid serverId, int page, int size);

	Task UnBanUser(string token, Guid serverId, Guid bannedId);
    Task ChangeServerIconAsync(string token, Guid serverId, IFormFile iconFile);
    Task DeleteServerIconAsync(string token, Guid serverId);
	Task ChangeServerClosedAsync(string token, Guid serverId, bool isClosed, bool? isApproved);

    Task ApproveApplicationAsync(string token, Guid applicationId);
    Task RemoveApplicationServerAsync(string token, Guid applicationId);
    Task RemoveApplicationUserAsync(string token, Guid applicationId);
    Task<ServerApplicationsListResponseDTO> GetServerApplicationsAsync(string token, Guid serverId, int page, int size);
    Task<UserApplicationsListResponseDTO> GetUserApplicationsAsync(string token, int page, int size);

    Task<ServerPresetListResponseDTO> GetServerPresetsAsync(string token, Guid serverId);
    Task<SystemRolesFullListNoneChildsDTO> RolesFullListAsync(string token, Guid serverId);
    Task<ServerPresetItemDTO> CreatePresetAsync(string token, Guid serverId, Guid serverRoleId, Guid systemRoleId);
    Task DeletePresetAsync(string token, Guid serverId, Guid serverRoleId, Guid systemRoleId);


    Task<ServerInvitationResponseDTO> CreateInvitationToken(string token, Guid serverId, DateTime expiresAt);
}