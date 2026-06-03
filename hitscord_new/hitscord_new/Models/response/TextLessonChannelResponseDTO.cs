namespace hitscord.Models.response;

public class TextLessonChannelResponseDTO
{
	public required Guid ChannelId { get; set; }
	public required string ChannelName { get; set; }
	public required bool CanCreateTasks { get; set; }
}