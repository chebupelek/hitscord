using hitscord.Models.other;

namespace hitscord.Models.response;

public class ServerInfoDTO
{
	public required Guid ServerId { get; set; }
	public required string ServerName { get; set; }
	public required ServerTypeEnum ServerType { get; set; }
	public FileMetaResponseDTO? Icon { get; set; }
	public required bool IsClosed { get; set; }
	public required List<RolesItemDTO> Roles { get; set; }
	public required List<UserServerRoles> UserRoles { get; set; }
	public required bool IsCreator { get; set; }
	public required SettingsDTO Permissions { get; set; }
	public required bool IsNotifiable { get; set; }
	public required List<ServerUserDTO> Users { get; set; }
	public required List<ChannelGroupResponseDTO> ChannelGroups { get; set; }
}

public class ChannelGroupResponseDTO
{
	public Guid? GroupId { get; set; }
	public string? GroupName { get; set; }
	public required int Position { get; set; }

	public required List<ChannelWrapperDTO> Channels { get; set; }
}

public class ChannelWrapperDTO
{
	public required int Position { get; set; }
	public required string Type { get; set; }
	public TextChannelResponseDTO? TextChannel { get; set; }
	public VoiceChannelResponseDTO? VoiceChannel { get; set; }
	public NotificationChannelResponseDTO? NotificationChannel { get; set; }
	public VoiceChannelResponseDTO? PairVoiceChannel { get; set; }
}