using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class ChannelNotificatedDbModel
{
	[Required]
	public required Guid RoleId { get; set; }
	[ForeignKey(nameof(RoleId))]
	public RoleDbModel Role { get; set; }

	[Required]
	public required Guid NotificationChannelId { get; set; }
	[ForeignKey(nameof(NotificationChannelId))]
	public NotificationChannelDbModel NotificationChannel { get; set; }
}
