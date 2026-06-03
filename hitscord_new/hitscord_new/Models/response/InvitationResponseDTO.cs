namespace hitscord.Models.response;

public class InvitationResponseDTO
{
    public required Guid ServerId { get; set; }
    public required Guid InvitationId { get; set; }
    public Guid? CreatorId { get; set; }
	public required string Token { get; set; }
	public DateTime? ExpiresAt { get; set; }
	public required bool IsRevoked { get; set; }
}