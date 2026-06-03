namespace hitscord.Models.response;

public class TextQueueChannelResponseDTO
{
	public required Guid ChannelId { get; set; }
	public required string ChannelName { get; set; }
	public required bool ChannelCanJoinQueue { get; set; }
	public required bool ChannelCanTakeFromQueue { get; set; }
	public required bool IsNotifiable { get; set; }
	public required int NonReadedCount { get; set; }
	public required int NonReadedTaggedCount { get; set; }
	public required long LastReadedMessageId { get; set; }
}