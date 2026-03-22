namespace hitscord.Models.response;

public class ChatMessageReactionFullDTO
{
	public required Guid Id { get; set; }
	public required Guid ChatId { get; set; }
	public required long MessageId { get; set; }
	public required Guid AuthorId { get; set; }
	public required DateTime CreatedAt { get; set; }
	public required string ReactionCode { get; set; }
}