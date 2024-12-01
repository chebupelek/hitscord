using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

public class ChannelDbModel
{
    public ChannelDbModel()
    {
        Id = Guid.NewGuid();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public required string Name { get; set; }

    public required List<RoleEnum> CanRead { get; set; }
}