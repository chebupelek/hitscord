namespace hitscord.Models.response;

public class ChatListItemDTO
{
    public required Guid ChatId { get; set; }
    public required string ChatName { get; set; }
	public required int NonReadedCount { get; set; }
	public required int NonReadedTaggedCount { get; set; }
	public required long LastReadedMessageId { get; set; }
	public FileMetaResponseDTO? Icon { get; set; }
}