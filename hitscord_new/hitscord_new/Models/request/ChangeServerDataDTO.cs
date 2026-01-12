using hitscord.Models.other;

namespace hitscord.Models.request;

public class ChangeServerDataDTO
{
	public required Guid ServerId { get; set; }
	public required bool Name { get; set; }
	public required bool serverType { get; set; }
	public required bool IsClosed { get; set; }
}