namespace hitscord.Models.db;

public class TextLessonChannelDbModel : ChannelDbModel
{
    public required ICollection<LessonChannelMessageDbModel> Messages { get; set; }
	public required ICollection<ChannelCanMakeTasksDbModel> ChannelCanMakeTasks { get; set; }

	public DateTime? DeleteTime { get; set; }
}