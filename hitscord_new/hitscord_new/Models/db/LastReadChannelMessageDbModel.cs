using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class LastReadChannelMessageDbModel
{
	[Required]
	public required Guid UserId { get; set; }
	[ForeignKey(nameof(UserId))]
	public UserDbModel User { get; set; }

	[Required]
	public required Guid TextChannelId { get; set; }
	[ForeignKey(nameof(TextChannelId))]
	public  TextChannelDbModel TextChannel { get; set; }

	public required long LastReadedMessageId { get; set; }
}
