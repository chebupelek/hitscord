using hitscord_net.Models.DBModels;

namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class ServerUserDTO
{
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required string UserTag { get; set; }
    public required RoleEnum Role { get; set; }
}