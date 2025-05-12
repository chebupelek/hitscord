using hitscord.Models.db;

namespace hitscord.Models.response;

public class ServerInfoDTO
{
	public required Guid ServerId { get; set; }
	public required string ServerName { get; set; }
	public required List<RolesItemDTO> Roles { get; set; }
	public required Guid UserRoleId { get; set; }
	public required string UserRole { get; set; }
	public required bool IsCreator { get; set; }
	public required bool CanChangeRole { get; set; }
	public required bool CanDeleteUsers { get; set; }
	public required bool CanWorkWithChannels { get; set; }
	public required bool CanMuteOthers { get; set; }
	public required List<ServerUserDTO> Users { get; set; }
	public required ChannelListDTO Channels { get; set; }
}
