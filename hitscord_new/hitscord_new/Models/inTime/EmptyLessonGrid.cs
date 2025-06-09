namespace hitscord.Models.inTime;

public class EmptyLessonGrid : LessonGridAbstract
{
	public required Object type { get; set; }
	public required int lessonNumber { get; set; }
	public required long starts { get; set; }
	public required long ends { get; set; }
}
