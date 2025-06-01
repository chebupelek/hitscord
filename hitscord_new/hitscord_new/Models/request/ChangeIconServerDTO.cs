using HitscordLibrary.Models.other;

namespace hitscord.Models.request;

public class ChangeIconServerDTO
{
	public required Guid ServerId { get; set; }
	public required IFormFile Icon { get; set; }
}