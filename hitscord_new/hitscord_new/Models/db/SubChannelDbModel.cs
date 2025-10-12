using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class SubChannelDbModel : TextChannelDbModel
{
	[Required]
	public required long ChannelMessageId { get; set; }
	[Required]
	public required Guid TextChannelId { get; set; }
	public ClassicChannelMessageDbModel ChannelMessage { get; set; }
	public required ICollection<ChannelCanUseDbModel> ChannelCanUse { get; set; }
}