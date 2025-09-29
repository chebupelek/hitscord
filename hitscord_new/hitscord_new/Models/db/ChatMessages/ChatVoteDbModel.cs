using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.db;

public class ChatVoteDbModel : ChatMessageDbModel
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
	public required ICollection<ChatVoteVariantDbModel> Variants { get; set; }
}