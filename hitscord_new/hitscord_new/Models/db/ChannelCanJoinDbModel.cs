using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class ChannelCanJoinDbModel
{
	[Required]
	public required Guid RoleId { get; set; }
	[ForeignKey(nameof(RoleId))]
	public RoleDbModel Role { get; set; }

	[Required]
	public required Guid VoiceChannelId { get; set; }
	[ForeignKey(nameof(VoiceChannelId))]
	public VoiceChannelDbModel VoiceChannel { get; set; }
}
