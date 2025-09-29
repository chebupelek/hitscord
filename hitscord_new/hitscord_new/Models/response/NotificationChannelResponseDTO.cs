namespace hitscord.Models.response;

public class NotificationChannelResponseDTO
{
    public required Guid ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required bool CanWrite { get; set; }
	public required bool IsNotificated { get; set; }
    public required bool IsNotifiable { get; set; }
	public required int NonReadedCount { get; set; }
	public required int NonReadedTaggedCount { get; set; }
	public required long LastReadedMessageId { get; set; }
}