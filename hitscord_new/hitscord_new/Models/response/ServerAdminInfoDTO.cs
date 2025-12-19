using hitscord.Models.other;

namespace hitscord.Models.response;

public class ServerAdminInfoDTO
{
	public required Guid ServerId { get; set; }
	public required string ServerName { get; set; }
	public required ServerTypeEnum ServerType { get; set; }
	public FileMetaResponseDTO? Icon { get; set; }
	public required bool IsClosed { get; set; }
	public required List<RolesAdminItemDTO> Roles { get; set; }
	public required List<ServerUserAdminDTO> Users { get; set; }
	public required ChannelAdminListDTO Channels { get; set; }
}
