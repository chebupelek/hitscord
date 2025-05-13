namespace hitscord.Models.response;

public class ApplicationsListItem
{
	public required Guid Id { get; set; }
	public required UserResponseDTO User {get; set;}
	public required DateTime CreatedAt {get; set;}
}