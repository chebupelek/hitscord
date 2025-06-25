namespace hitscord.Models.response;

public class UserApplicationResponseDTO
{
	public required Guid ApplicationId { get; set; }
	public required Guid ServerId { get; set; }
	public required string ServerName { get; set; }
	public required DateTime CreatedAt { get; set; }
}