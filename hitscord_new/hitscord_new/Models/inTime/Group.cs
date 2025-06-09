namespace hitscord.Models.inTime;

public class Group
{
    public required Guid id { get; set; }
    public required string name { get; set; }
	public required bool isSubgroup { get; set; }
	public Guid? facultyId { get; set; }
}
