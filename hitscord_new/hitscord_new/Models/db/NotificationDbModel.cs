using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class NotificationDbModel
{
	public NotificationDbModel()
	{
		Id = Guid.NewGuid();
	}

	[Key]
	public Guid Id { get; set; }

	[Required]
	public Guid UserId { get; set; }
	[ForeignKey(nameof(UserId))]
	public UserDbModel User { get; set; }

	[MinLength(1)]
	public required string Text { get; set; }
	public required DateTime CreatedAt { get; set; }

	public required bool IsReaded { get; set; }

	public Guid? ServerId { get; set; }

	public Guid? TextChannelId { get; set; }

	public Guid? ChatId { get; set; }
}