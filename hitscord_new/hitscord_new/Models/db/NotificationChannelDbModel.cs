namespace hitscord.Models.db;

public class NotificationChannelDbModel : TextChannelDbModel
{
	public required ICollection<ChannelNotificatedDbModel> ChannelNotificated { get; set; }
}