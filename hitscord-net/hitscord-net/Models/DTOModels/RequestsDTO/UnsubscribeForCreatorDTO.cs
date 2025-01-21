namespace hitscord_net.Models.DTOModels.RequestsDTO;

public class UnsubscribeForCreatorDTO
{
    public required Guid serverId {  get; set; }
    public required Guid newCreatorId { get; set; }
}