using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class QueueTakeDbModel
{
	[Required]
	public required Guid TextQueueChannelId { get; set; }

	[ForeignKey(nameof(TextQueueChannelId))]
	public TextQueueChannelDbModel TextQueueChannel { get; set; }

	[Required]
	public required Guid TakerId { get; set; }

	[ForeignKey(nameof(TakerId))]
	public UserDbModel Taker { get; set; }

	[Required]
	public required Guid FromQueueId { get; set; }

	[ForeignKey(nameof(FromQueueId))]
	public UserDbModel FromQueue { get; set; }
}