using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class NonNotifiableChannelDbModel
{
	[Required]
	public required Guid UserServerId { get; set; }
	[ForeignKey(nameof(UserServerId))]
	public UserServerDbModel UserServer { get; set; }

	[Required]
	public required Guid TextChannelId { get; set; }
	[ForeignKey(nameof(TextChannelId))]
	public TextChannelDbModel TextChannel { get; set; }
}
