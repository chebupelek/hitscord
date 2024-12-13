using hitscord_net.Models.DBModels;

namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class ServerInfoDTO
{
    public required Guid ServerId { get; set; }
    public required string ServerName { get; set; }
    public required List<RoleDbModel> Roles { get; set; }
    public required Guid UserRoleId { get; set; }
    public required String UserRole { get; set; }
    public required List<ServerUserDTO> Users { get; set;}
    public required ChannelListDTO Channels { get; set; }
}
