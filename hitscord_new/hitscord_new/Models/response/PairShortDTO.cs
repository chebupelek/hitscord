using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class PairShortDTO
{
	public required Guid Id { get; set; }
	public required Guid ServerId { get; set; }
	public required string ServerName { get; set; }
	public required Guid PairVoiceChannelId { get; set; }
	public required string PairVoiceChannelName { get; set; }
	public required List<RolesItemDTO> Roles { get; set; }
	public string? Note { get; set; }
}
