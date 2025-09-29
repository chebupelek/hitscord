using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class ChannelCanWriteSubDbModel
{
	[Required]
	public required Guid RoleId { get; set; }
	[ForeignKey(nameof(RoleId))]
	public RoleDbModel Role { get; set; }

	[Required]
	public required Guid TextChannelId { get; set; }
	[ForeignKey(nameof(TextChannelId))]
	public TextChannelDbModel TextChannel { get; set; }
}
