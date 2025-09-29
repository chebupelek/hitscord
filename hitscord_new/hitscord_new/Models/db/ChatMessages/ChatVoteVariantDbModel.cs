using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using hitscord.Models.db;

namespace hitscord.Models.db;

public class ChatVoteVariantDbModel
{
	public ChatVoteVariantDbModel()
	{
		Id = Guid.NewGuid();
	}

	[Key]
	public Guid Id { get; set; }

	[Required]
	public required int Number { get; set; }

	[Required]
	[MinLength(1)]
	[MaxLength(5000)]
	public required string Content { get; set; }

	[Required]
	public required long VoteId { get; set; }
	[Required]
	public required Guid ChatId { get; set; }
	[ForeignKey(nameof(VoteId) + "," + nameof(ChatId))]
	public ChatVoteDbModel Vote { get; set; }

	public ICollection<ChatVariantUserDbModel> UsersVariants { get; set; }
}