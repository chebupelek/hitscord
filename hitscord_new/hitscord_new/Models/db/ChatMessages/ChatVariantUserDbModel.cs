using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using hitscord.Models.db;

namespace hitscord.Models.db;

public class ChatVariantUserDbModel
{
	public ChatVariantUserDbModel()
	{
		Id = Guid.NewGuid();
	}

	[Key]
	public Guid Id { get; set; }

	[Required]
	public required Guid UserId { get; set; }
	[ForeignKey(nameof(UserId))]
	public UserDbModel User { get; set; }

	[Required]
	public required Guid VariantId { get; set; }
	[ForeignKey(nameof(VariantId))]
	public ChatVoteVariantDbModel Variant { get; set; }
}