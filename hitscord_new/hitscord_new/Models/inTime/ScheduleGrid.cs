namespace hitscord.Models.inTime;

public class ScheduleGrid
{
    public required List<ScheduleColumn?> grid { get; set; }
    public required List<GroupWithFaculty?> groups { get; set; }
	public required List<Professor?> professors { get; set; }
	public required List<AudienceWithBuilding?> audiences { get; set; }
	public required string hash { get; set; }
}
