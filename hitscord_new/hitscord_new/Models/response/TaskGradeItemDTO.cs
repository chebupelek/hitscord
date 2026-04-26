using hitscord.Models.db;

namespace hitscord.Models.response;

public class TaskGradeItemDTO
{
	public required Guid ServerId { get; set; }
	public required Guid ChannelId { get; set; }
	public required long TaskId { get; set; }
	public long? SolutionId { get; set; }
	public required Guid UserId { get; set; }
	public required Guid GraderId { get; set; }
	public required int Grade { get; set; }
	public required DateTime GradeDate { get; set; }
}
