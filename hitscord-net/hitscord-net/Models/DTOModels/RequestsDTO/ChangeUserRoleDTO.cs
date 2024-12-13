using hitscord_net.Models.DBModels;

namespace hitscord_net.Models.DTOModels.RequestsDTO;

public class ChangeUserRoleDTO
{
    public required Guid ServerId { get; set; }
    public required Guid UserId { get; set; }
    public required Guid Role { get; set; }
}