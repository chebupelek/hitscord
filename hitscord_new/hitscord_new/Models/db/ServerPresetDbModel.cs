using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class ServerPresetDbModel
{
    [Required]
    public required Guid SystemRoleId { get; set; }
    [ForeignKey(nameof(SystemRoleId))]
    public SystemRoleDbModel SystemRole { get; set; }

	[Required]
	public required Guid ServerRoleId { get; set; }
	[ForeignKey(nameof(ServerRoleId))]
	public RoleDbModel ServerRole { get; set; }
}