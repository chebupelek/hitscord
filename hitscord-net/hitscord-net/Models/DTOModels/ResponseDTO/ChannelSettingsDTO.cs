using hitscord_net.Models.DBModels;

namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class ChannelSettingsDTO
{
    public required List<RoleEnum> CanRead { get; set; }
    public required List<RoleEnum> CanWrite { get; set; }
}