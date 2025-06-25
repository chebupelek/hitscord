using HitscordLibrary.Models.other;

namespace hitscord.Models.request;

public class ChangeServerIsClosedDTO
{
	public required Guid ServerId { get; set; }
	public required bool IsClosed { get; set; }
	public bool? IsApprove { get; set; }
}