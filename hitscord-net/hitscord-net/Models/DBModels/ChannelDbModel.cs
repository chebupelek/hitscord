using hitscord_net.Models.InnerModels;
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
    public required ChannelTypeEnum Type { get; set; }
    public required List<RoleEnum> CanRead { get; set; }
    public required List<RoleEnum> CanWrite { get; set; }
}