namespace hitscord.Models.response;

public class SolutionMessageResponseDTO : MessageResponceDTO
{
	public required string Description { get; set; }

	public DateTime? UpdatedAt { get; set; }

	public required long TaskId { get; set; }

	public int? Grade { get; set; }

	public DateTime? GradeDate { get; set; }

	public Guid? GradeAuthorId { get; set; }

	public required List<FileMetaResponseDTO> Files { get; set; }

	// Для преподавателей
	public string? AuthorName { get; set; }
}