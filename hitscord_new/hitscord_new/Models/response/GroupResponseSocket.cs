using hitscord.Models.other;

namespace hitscord.Models.response;

public class GroupResponseSocket
{
    public required Guid ServerId { get; set; }
	public required Guid GroupId { get; set; }
    public required string GroupName { get; set; }
    public required int Position { get; set; }
}