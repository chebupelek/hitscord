using hitscord.Models.other;

namespace hitscord.Models.request;

public class UpdateGroupDTO
{
    public required Guid GroupId {  get; set; }
    public string? Name { get; set; }
	public int? Position { get; set; }
}
