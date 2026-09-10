using hitscord.Models.db;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.db;

public class UserDeviceTokenDbModel
{
	[Key]
	public Guid Id { get; set; }

	public Guid UserId { get; set; }

	[ForeignKey(nameof(UserId))]
	public UserDbModel User { get; set; }

	[Required]
	public string Token { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}