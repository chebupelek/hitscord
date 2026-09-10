using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.db;

public class TextQueueChannelDbModel : TextChannelDbModel
{
	public required ICollection<ChannelCanJoinQueueDbModel> ChannelCanJoinQueue { get; set; }
	public required ICollection<ChannelCanTakeFromQueueDbModel> ChannelCanTakeFromQueue { get; set; }
	public required ICollection<QueueItemDbModel> Queue { get; set; }
	public required ICollection<QueueTakeDbModel> Takes { get; set; }
}

public class QueueItemDbModel
{
	public QueueItemDbModel()
	{
		Id = Guid.NewGuid();
	}

	[Key]
	public Guid Id { get; set; }

	public Guid ChannelId { get; set; }
	[ForeignKey(nameof(ChannelId))]
	public TextQueueChannelDbModel Channel { get; set; }

	public required Guid UserId { get; set; }
	[ForeignKey(nameof(UserId))]
	public UserDbModel User { get; set; }

	public required int Position { get; set; }
	public required DateTime CreatedAt { get; set; }
}