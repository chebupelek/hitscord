using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class ChannelCanGiveGradesDbModel
{
	[Required]
	public required Guid RoleId { get; set; }
	[ForeignKey(nameof(RoleId))]
	public RoleDbModel Role { get; set; }

	[Required]
	public required Guid TextLessonChannelId { get; set; }
	[ForeignKey(nameof(TextLessonChannelId))]
	public TextLessonChannelDbModel TextLessonChannel { get; set; }
}
