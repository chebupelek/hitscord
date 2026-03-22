using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace hitscord.Models.db;

public class ChannelMessageReactionDbModel
{
    public ChannelMessageReactionDbModel()
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

	public required Guid ChannelMessageId { get; set; }
	[ForeignKey(nameof(ChannelMessageId))]
	public ChannelMessageDbModel ChannelMessage { get; set; }

	public required string ReactionCode { get; set; }
}