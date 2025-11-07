using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class SystemRoleItemDTO
{
	public required Guid Id { get; set; }
	public required string Name { get; set; }
	public required SystemRoleTypeEnum Type { get; set; }
	public List<SystemRoleItemDTO> ChildRoles { get; set; } = new();
}