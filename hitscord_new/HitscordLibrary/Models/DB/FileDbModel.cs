using System.ComponentModel.DataAnnotations;

namespace HitscordLibrary.Models.db;

public class FileDbModel
{
    [Key]
    public required Guid Id { get; set; }
    public required string Path { get; set; } // путь до файла
    public required string Name { get; set; } // имя с расширением (например image.png)
    public required string Type { get; set; } // мим тип
}
