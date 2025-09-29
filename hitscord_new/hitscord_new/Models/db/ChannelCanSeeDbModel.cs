using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class ChannelCanSeeDbModel
{
	[Required]
	public required Guid RoleId { get; set; }
	[ForeignKey(nameof(RoleId))]
	public RoleDbModel Role { get; set; }

	[Required]
	public required Guid ChannelId { get; set; }
	[ForeignKey(nameof(ChannelId))]
	public ChannelDbModel Channel { get; set; }
}
