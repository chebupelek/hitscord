using System.ComponentModel.DataAnnotations;

namespace HitscordLibrary.Models.db;

public class FileDbModel
{
    [Key]
    public required Guid Id { get; set; }
    public required string Path { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
	public required long Size { get; set; }
	public required Guid Creator { get; set; }
	public required bool IsApproved { get; set; }
	public required DateTime CreatedAt { get; set; }
}
