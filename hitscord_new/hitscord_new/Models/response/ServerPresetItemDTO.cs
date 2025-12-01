using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class ServerPresetItemDTO
{
	public required Guid ServerRoleId { get; set; }
	public required string ServerRoleName { get; set; }
	public required Guid SystemRoleId { get; set; }
	public required string SystemRoleName { get; set; }
	public required SystemRoleTypeEnum SystemRoleType { get; set; }
}