namespace hitscord.Models.other;

public class InvitationPayload
{
	public required string ServerName { get; set; }
	public Guid? ServerIconId { get; set; }
	public required string InvitationToken { get; set; }
}
