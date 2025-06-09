namespace hitscord.Models.inTime;

public class GroupWithFaculty
{
    public required Guid id { get; set; }
    public required string name { get; set; }
	public required bool isSubgroup { get; set; }
	public required Faculty faculty { get; set; }
}
