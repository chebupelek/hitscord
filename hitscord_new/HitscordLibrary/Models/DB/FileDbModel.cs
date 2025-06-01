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
}
