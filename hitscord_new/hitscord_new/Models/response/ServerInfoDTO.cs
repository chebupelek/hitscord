namespace hitscord.Models.response;

public class ServerInfoDTO
{
	public required Guid ServerId { get; set; }
	public required string ServerName { get; set; }
	public FileMetaResponseDTO? Icon { get; set; }
	public required bool IsClosed { get; set; }
	public required List<RolesItemDTO> Roles { get; set; }
	public required List<UserServerRoles> UserRoles { get; set; }
	public required bool IsCreator { get; set; }
	public required SettingsDTO Permissions { get; set; }
	public required bool IsNotifiable { get; set; }
	public required List<ServerUserDTO> Users { get; set; }
	public required ChannelListDTO Channels { get; set; }
}
