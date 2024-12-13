using hitscord_net.Models.DBModels;

namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class ChannelSettingsDTO
{
    public required List<RoleDbModel> CanRead { get; set; }
    public required List<RoleDbModel> CanWrite { get; set; }
}