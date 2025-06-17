using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class NewPairResponseDTO
{
	public required Guid Id { get; set; }
	public required Guid ScheduleId { get; set; }
	public required string ServerName { get; set; }
	public required string PairVoiceChannelName { get; set; }
	public required List<RoleDbModel> Roles { get; set; }
	public string? Note { get; set; }
	public required string Date { get; set; }
	public required int LessonNumber { get; set; }
	public string? Title { get; set; }
}