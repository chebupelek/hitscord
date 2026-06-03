namespace hitscord.Models.response;

public class UserInvitationDTO
{
	public required Guid UserId { get; set; }
    public Guid? InvitationId { get; set; }
    public required DateTime JoinDate { get; set; }
}
