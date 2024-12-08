namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class ServersListItemDTO
{
    public required Guid ServerId { get; set; }
    public required string ServerName { get; set; }
}