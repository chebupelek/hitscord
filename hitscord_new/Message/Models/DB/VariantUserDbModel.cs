using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using HitscordLibrary.Migrations.Files;

namespace Message.Models.DB;

public class VariantUserDbModel
{
	public VariantUserDbModel()
	{
		Id = Guid.NewGuid();
	}

	[Key]
	public Guid Id { get; set; }

	public Guid? VariantId { get; set; }
	[ForeignKey(nameof(VariantId))]
	public VoteVariantDbModel? Variant { get; set; }

	public required Guid UserId { get; set; }
}