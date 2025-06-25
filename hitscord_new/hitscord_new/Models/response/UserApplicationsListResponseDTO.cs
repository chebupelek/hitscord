namespace hitscord.Models.response;

public class UserApplicationsListResponseDTO
{
	public List<UserApplicationResponseDTO>? Applications { get; set; }
	public required int Page {get; set;}
	public required int Size { get; set; }
	public required int Total { get; set; }
}