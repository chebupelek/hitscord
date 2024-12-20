using hitscord_net.Models.InnerModels;

namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class NewChannelResponseDTO
{
    public required bool Create {  get; set; }
    public required Guid ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required ChannelTypeEnum ChannelType { get; set; }
}