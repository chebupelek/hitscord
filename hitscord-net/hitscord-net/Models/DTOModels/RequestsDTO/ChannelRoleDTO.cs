using hitscord_net.Models.DBModels;

namespace hitscord_net.Models.DTOModels.RequestsDTO;

public class ChannelRoleDTO
{
    public required Guid ChannelId { get; set; }
    public required Guid Role { get; set; }
}