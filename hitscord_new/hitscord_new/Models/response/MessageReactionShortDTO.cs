namespace hitscord.Models.response;

public class MessageReactionShortDTO
{
	public required Guid Id { get; set; }
	public Guid? AuthorId { get; set; }
	public required DateTime CreatedAt { get; set; }
	public required string ReactionCode { get; set; }
}