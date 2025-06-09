using static System.Runtime.InteropServices.JavaScript.JSType;

namespace hitscord.Models.inTime;

public class ScheduleColumn
{
	public required DateOnly date { get; set; }
	public List<LessonGrid>? lessons { get; set; }
}
