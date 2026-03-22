using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace hitscord.Models.db;

public class ChatMessageReactionDbModel
{
    public ChatMessageReactionDbModel()
    {
        CreatedAt = DateTime.UtcNow;
		Id = Guid.NewGuid();
	}
	[Key]
	public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

	public Guid? AuthorId { get; set; }
	[ForeignKey(nameof(AuthorId))]
	public UserDbModel? Author { get; set; }

	public required Guid ChatMessageId { get; set; }
	[ForeignKey(nameof(ChatMessageId))]
	public ChatMessageDbModel ChatMessage { get; set; }

	public required string ReactionCode { get; set; }
}