using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace hitscord.Models.db;

public class LessonChannelMessageSolutionDbModel : LessonChannelMessageDbModel
{
	[MinLength(1)]
	[MaxLength(10000)]
	public required string Description { get; set; }
	public DateTime? UpdatedAt { get; set; }
	public int? Grade { get; set; }
	public DateTime? GradeDate { get; set; }
	public Guid? GradeAuthorId { get; set; }
	[ForeignKey(nameof(GradeAuthorId))]
	public UserDbModel? GradeAuthor { get; set; }
}