using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class PairUserDbModel
{
	public PairUserDbModel()
	{
		Id = Guid.NewGuid();
	}

	[Key]
	public Guid Id { get; set; }

	[Required]
	public Guid UserId { get; set; }

	[ForeignKey(nameof(UserId))]
	public UserDbModel User { get; set; }

	[Required]
	public Guid PairId { get; set; }

	[ForeignKey(nameof(PairId))]
	public PairDbModel Pair { get; set; }

	public required DateTime TimeEnter { get; set; }
	public DateTime? TimeLeave { get; set; }
	public DateTime? TimeUpdate { get; set; }
}