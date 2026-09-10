using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IServerService
{
    Task<ServerDbModel> CheckServerExistAsync(Guid serverId, bool includeChannels);
    Task<ServerDbModel> GetServerFullModelAsync(Guid serverId);

    Task RedisUpdateFullServerAsync();

	Task<ServerIdDTO> CreateServerAsync(Guid UserId, string severName, ServerTypeEnum? type);
	Task SubscribeAsync(Guid UserId, string invitationToken, string? userName);
    Task UnsubscribeAsync(Guid serverId, Guid UserId);
    Task UnsubscribeForCreatorAsync(Guid serverId, Guid UserId, Guid newCreatorId);
    Task DeleteServerAsync(Guid serverId, Guid UserId);
    Task<ServersListDTO> GetServerListAsync(Guid UserId);
    Task AddRoleToUserAsync(Guid UserId, Guid serverId, Guid userId, Guid roleId);
	Task RemoveRoleFromUserAsync(Guid UserId, Guid serverId, Guid userId, Guid roleId);
	Task<ServerInfoDTO> GetServerInfoAsync(Guid UserId, Guid serverId);
    Task DeleteUserFromServerAsync(Guid UserId, Guid serverId, Guid userId, string? banReason);
    Task ChangeServerNameAsync(Guid serverId, Guid UserId, string name);
    Task ChangeUserNameAsync(Guid serverId, Guid UserId, string name);
    Task ChangeNonNotifiableServerAsync(Guid UserId, Guid serverId);
    Task<BanListDTO> GetBannedListAsync(Guid UserId, Guid serverId, int page, int size);

	Task UnBanUser(Guid UserId, Guid serverId, Guid bannedId);
    Task ChangeServerIconAsync(Guid UserId, Guid serverId, IFormFile iconFile);
    Task DeleteServerIconAsync(Guid UserId, Guid serverId);
	Task ChangeServerClosedAsync(Guid UserId, Guid serverId, bool isClosed, bool? isApproved);

    Task ApproveApplicationAsync(Guid UserId, Guid applicationId);
    Task RemoveApplicationServerAsync(Guid UserId, Guid applicationId);
    Task RemoveApplicationUserAsync(Guid UserId, Guid applicationId);
    Task<ServerApplicationsListResponseDTO> GetServerApplicationsAsync(Guid UserId, Guid serverId, int page, int size);
    Task<UserApplicationsListResponseDTO> GetUserApplicationsAsync(Guid UserId, int page, int size);

    Task<ServerPresetListResponseDTO> GetServerPresetsAsync(Guid UserId, Guid serverId);
    Task<SystemRolesFullListNoneChildsDTO> RolesFullListAsync(Guid UserId, Guid serverId);
    Task<ServerPresetItemDTO> CreatePresetAsync(Guid UserId, Guid serverId, Guid serverRoleId, Guid systemRoleId);
    Task DeletePresetAsync(Guid UserId, Guid serverId, Guid serverRoleId, Guid systemRoleId);


    Task<ServerInvitationResponseDTO> CreateInvitationToken(Guid UserId, Guid serverId, DateTime? expiresAt);
    Task<InvitationDataResponseDTO> GetInvitationTokensDataAsync(Guid UserId, Guid serverId);
    Task RevokeTokenAsync(Guid UserId, Guid serverId, Guid invitationId);
}