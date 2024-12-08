namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class ServerUserDTO
{
    public required Guid UserId { get; set; }
    public string UserName { get; set; }
    public string UserTag { get; set; }
}