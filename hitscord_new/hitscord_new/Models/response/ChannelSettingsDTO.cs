using hitscord.Models.response;

namespace hitscord.Models.response;

public class ChannelSettingsDTO
{
    public required List<RolesItemDTO> CanRead { get; set; }
    public required List<RolesItemDTO> CanWrite { get; set; }
}