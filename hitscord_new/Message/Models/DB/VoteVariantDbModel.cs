using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using HitscordLibrary.Migrations.Files;

namespace Message.Models.DB;

public class VoteVariantDbModel
{
	public VoteVariantDbModel()
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

	public Guid? VoteId { get; set; }
	[ForeignKey(nameof(VoteId))]
	public VoteDbModel? Vote { get; set; }
}