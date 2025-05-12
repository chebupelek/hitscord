namespace hitscord.Models.response;

public class NotificationChannelResponseDTO
{
    public required Guid ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required bool CanWrite { get; set; }
	public required bool IsNotificated { get; set; }
}