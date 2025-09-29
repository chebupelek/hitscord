using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class UserChatDbModel
{
	[Required]
	public required Guid UserId { get; set; }
	[ForeignKey(nameof(UserId))]
	public UserDbModel User { get; set; }

	[Required]
	public required Guid ChatId { get; set; }
	[ForeignKey(nameof(ChatId))]
	public ChatDbModel Chat { get; set; }

	public required bool NonNotifiable { get; set; }
}
