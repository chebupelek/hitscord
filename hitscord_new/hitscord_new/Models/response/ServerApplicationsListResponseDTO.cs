namespace hitscord.Models.response;

public class ServerApplicationsListResponseDTO
{
	public List<ServerApplicationResponseDTO>? Applications { get; set; }
	public required int Page {get; set;}
	public required int Size { get; set; }
	public required int Total { get; set; }
}