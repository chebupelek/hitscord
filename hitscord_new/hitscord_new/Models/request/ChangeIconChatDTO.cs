namespace hitscord.Models.request;

public class ChangeIconChatDTO
{
	public required Guid ChatID { get; set; }
	public required IFormFile Icon { get; set; }
}