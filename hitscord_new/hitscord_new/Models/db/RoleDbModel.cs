using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class RoleDbModel
{
    public RoleDbModel()
    {
        Id = Guid.NewGuid();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    public required RoleEnum Role { get; set; }

    [Required]
    public required Guid ServerId { get; set; }

    [ForeignKey(nameof(ServerId))]
    public ServerDbModel Server { get; set; }
}
