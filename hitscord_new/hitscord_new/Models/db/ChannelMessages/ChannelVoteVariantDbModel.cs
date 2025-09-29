using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using hitscord.Models.db;

namespace hitscord.Models.db;

public class ChannelVoteVariantDbModel
{
	public ChannelVoteVariantDbModel()
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
	public required Guid TextChannelId { get; set; }
	[ForeignKey(nameof(VoteId) + "," + nameof(TextChannelId))]
	public ChannelVoteDbModel Vote { get; set; }

	public ICollection<ChannelVariantUserDbModel> UsersVariants { get; set; }
}