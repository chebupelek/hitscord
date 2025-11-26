using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace hitscord.Models.response;

public class SystemRoleShortItemDTO
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Guid? Id { get; set; }

	public required string Name { get; set; }
	public required SystemRoleTypeEnum Type { get; set; }
}