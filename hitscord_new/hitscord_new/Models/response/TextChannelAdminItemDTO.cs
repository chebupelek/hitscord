using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class TextChannelAdminItemDTO
{
	public required Guid ChannelId { get; set; }
	public required string ChannelName { get; set; }
	public required Guid ServerID { get; set; }
	public required string ServerName { get; set; }
	public required DateTime DeleteTime { get; set; }
}