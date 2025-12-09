using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class SystemRoleNoneListItemDTO
{
	public required Guid Id { get; set; }
	public required string Name { get; set; }
	public required SystemRoleTypeEnum Type { get; set; }
	public Guid? ParentId { get; set; }
	public string? ParentName { get; set; }
}