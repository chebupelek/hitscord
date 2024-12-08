using hitscord_net.Models.InnerModels;
using Microsoft.AspNetCore.Mvc;

namespace hitscord_net.Models.DTOModels.RequestsDTO;

public class CreateChannelDTO
{
    public required Guid ServerId {  get; set; }
    public required string Name { get; set; }
    public required ChannelTypeEnum ChannelType { get; set; }
}
