using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Message.Models.DB;

public class VoteDbModel : MessageDbModel
{
	[Required]
	[MinLength(1)]
	[MaxLength(5000)]
	public required string Title { get; set; }

	[MaxLength(5000)]
	public string? Content { get; set; }

	[Required]
	public required bool IsAnonimous { get; set; }
	[Required]
	public required bool Multiple { get; set; }

	public DateTime? Deadline { get; set; }

	[Required]
	public required List<VoteVariantDbModel> Variants { get; set; }
}