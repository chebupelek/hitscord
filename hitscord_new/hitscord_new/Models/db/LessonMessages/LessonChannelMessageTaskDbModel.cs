using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace hitscord.Models.db;

public class LessonChannelMessageTaskDbModel : LessonChannelMessageDbModel
{
	[MinLength(1)]
	[MaxLength(10000)]
	public required string Description { get; set; }
	public DateTime? UpdatedAt { get; set; }
	public DateTime? Deadline { get; set; }
	public ICollection<FileDbModel> Files { get; set; }
	public ICollection<RoleDbModel> AssignedRoles { get; set; }
}