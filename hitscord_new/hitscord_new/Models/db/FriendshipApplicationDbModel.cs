using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class FriendshipApplicationDbModel
{
	[Key]
	[Required]
	public required Guid Id { get; set; }

	[Required]
	public required Guid UserIdFrom { get; set; }
	[ForeignKey(nameof(UserIdFrom))]
	public UserDbModel UserFrom { get; set; }

	[Required]
	public required Guid UserIdTo { get; set; }
	[ForeignKey(nameof(UserIdTo))]
	public UserDbModel UserTo { get; set; }

	public required DateTime CreatedAt { get; set; }
}