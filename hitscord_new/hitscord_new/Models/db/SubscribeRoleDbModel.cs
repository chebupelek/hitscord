using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class SubscribeRoleDbModel
{
	[Required]
	public required Guid UserServerId { get; set; }
	[ForeignKey(nameof(UserServerId))]
	public UserServerDbModel UserServer { get; set; }

	[Required]
	public required Guid RoleId { get; set; }
	[ForeignKey(nameof(RoleId))]
	public RoleDbModel Role { get; set; }
}