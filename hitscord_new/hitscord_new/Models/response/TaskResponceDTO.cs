using hitscord.Models.db;

namespace hitscord.Models.response;

public class TaskResponceDTO
{
    public required string MessageType { get; set; }
	public Guid? ServerId { get; set; }
	public string? ServerName { get; set; }
	public Guid? ChannelId { get; set; }
	public string? ChannelName { get; set; }
	public required long Id { get; set; }
    public  Guid? AuthorId { get; set; }
    public required DateTime CreatedAt { get; set; }
	public required string Description { get; set; }
	public DateTime? UpdatedAt { get; set; }
	public DateTime? Deadline { get; set; }
	public List<FileMetaResponseDTO>? Files { get; set; }
	public List<Guid> AssignedRolesIds { get; set; }
}