using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class ServerPresetItemDTO
{
	public required Guid ServerRoleId { get; set; }
	public required string ServerRoleName { get; set; }
	public required Guid SystenRoleId { get; set; }
	public required string SystenRoleName { get; set; }
	public required SystemRoleTypeEnum SystenRoleType { get; set; }
}