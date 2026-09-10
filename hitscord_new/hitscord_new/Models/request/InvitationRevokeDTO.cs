using hitscord.Models.other;

namespace hitscord.Models.request;

public class InvitationRevokeDTO
{
	public required Guid ServerId { get; set; }
	public required Guid InvitationId { get; set; }
}