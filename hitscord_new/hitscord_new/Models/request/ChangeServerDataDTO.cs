using hitscord.Models.other;

namespace hitscord.Models.request;

public class ChangeServerDataDTO
{
	public required Guid ServerId { get; set; }
	public string? Name { get; set; }
	public ServerTypeEnum? ServerType { get; set; }
	public bool? IsClosed { get; set; }
	public Guid? NewCreatorId { get; set; }
}