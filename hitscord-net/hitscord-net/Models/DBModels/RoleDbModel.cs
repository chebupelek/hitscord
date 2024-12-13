using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

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
}
