namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class FriendshipApplicationDTO
{
    public required UserFriendshipDTO User {  get; set; }
    public required DateTime CreateDate { get; set; }
}