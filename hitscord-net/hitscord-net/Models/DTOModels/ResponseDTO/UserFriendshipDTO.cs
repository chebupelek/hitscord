namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class UserFriendshipDTO
{
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public required string UserTag { get; set;}
}