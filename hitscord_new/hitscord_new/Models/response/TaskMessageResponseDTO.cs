namespace hitscord.Models.response;

public class TaskMessageResponseDTO : MessageResponceDTO
{
	public required string Description { get; set; }

	public DateTime? UpdatedAt { get; set; }

	public DateTime? Deadline { get; set; }

	public required List<FileMetaResponseDTO> Files { get; set; }

	public bool AssignedToMe { get; set; }

	public bool SolutionSent { get; set; }

	public int? MyGrade { get; set; }

	// Только для преподавателей
	public int? SolutionsCount { get; set; }

	public List<RolesItemDTO>? AssignedRoles { get; set; }

	public List<ServerUserDTO>? AssignedUsers { get; set; }
}