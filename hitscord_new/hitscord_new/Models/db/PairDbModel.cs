using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class PairDbModel
{
	public PairDbModel()
	{
		Id = Guid.NewGuid();
	}

	[Key]
	public Guid Id { get; set; }

	public required Guid ScheduleId { get; set; }

	[Required]
	public Guid ServerId { get; set; }

	[ForeignKey(nameof(ServerId))]
	public ServerDbModel Server { get; set; }

	[Required]
	public Guid PairVoiceChannelId { get; set; }

	[ForeignKey(nameof(PairVoiceChannelId))]
	public PairVoiceChannelDbModel PairVoiceChannel { get; set; }

	public required List<RoleDbModel> Roles { get; set; }

	public string? Note { get; set; }

	public required ScheduleType Type { get; set; }
	public required Guid FilterId { get; set; }
	public required string Date { get; set; }
	public required long Starts { get; set; }
	public required long Ends { get; set; }
	public required int LessonNumber { get; set; }
	public string? Title { get; set; }
}