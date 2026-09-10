using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class ChannelCanJoinQueueDbModel
{
	[Required]
	public required Guid RoleId { get; set; }
	[ForeignKey(nameof(RoleId))]
	public RoleDbModel Role { get; set; }

	[Required]
	public required Guid TextQueueChannelId { get; set; }
	[ForeignKey(nameof(TextQueueChannelId))]
	public TextQueueChannelDbModel TextQueueChannel { get; set; }
}
