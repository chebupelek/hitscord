using hitscord.Models.db;

namespace hitscord.Models.response;

public class ChatInfoDTO
{
	public required Guid ChatId { get; set; }
	public required string ChatName { get; set; }
	public required int NonReadedCount { get; set; }
	public required int NonReadedTaggedCount { get; set; }
	public required long LastReadedMessageId { get; set; }
	public required bool NonNotifiable { get; set; }
	public FileMetaResponseDTO? Icon { get; set; }
	public required List<UserChatResponseDTO> Users { get; set; }
}
