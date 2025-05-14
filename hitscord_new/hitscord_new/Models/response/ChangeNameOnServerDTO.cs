namespace hitscord.Models.response;

public class ChangeNameOnServerDTO
{
	public required Guid ServerId { get; set; }
	public required Guid UserId { get; set; }
	public required string Name { get; set; }
}