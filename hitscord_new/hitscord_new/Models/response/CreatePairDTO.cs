using hitscord.Models.other;

namespace hitscord.Models.response;

public class CreatePairDTO
{
	public required Guid ScheduleId { get; set; }
	public required Guid PairVoiceChannelId { get; set; }
	public required List<Guid> RoleIds { get; set; }
	public string? Note { get; set; }
	public required ScheduleType Type { get; set; }
	public required Guid Id { get; set; }
	public required string Date { get; set; }
}