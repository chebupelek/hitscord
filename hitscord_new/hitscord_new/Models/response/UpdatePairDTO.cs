using hitscord.Models.other;

namespace hitscord.Models.response;

public class UpdatePairDTO
{
	public required Guid PairId { get; set; }
	public required List<Guid> RoleIds { get; set; }
	public string? Note { get; set; }
}