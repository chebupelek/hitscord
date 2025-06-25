namespace hitscord.Models.response;

public class ServerApplicationResponseDTO
{
	public required Guid ApplicationId { get; set; }
	public required Guid ServerId { get; set; }
	public required ProfileDTO User { get; set; }
	public required DateTime CreatedAt { get; set; }
}