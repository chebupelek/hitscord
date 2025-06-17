using hitscord.Models.response;

namespace hitscord.Models.inTime;

public class LessonGrid
{
	public required string type { get; set; }
	public Guid? id { get; set; }
    public string? title { get; set; }
	public required int lessonNumber { get; set; }
	public required long starts { get; set; }
	public required long ends { get; set; }
	public LessonType? lessonType { get; set; }
	public List<Group>? groups { get; set; }
	public Professor? professor { get; set; }
	public AudienceWithBuilding? audience { get; set; }
	public List<PairShortDTO>? Pairs { get; set; }
}
