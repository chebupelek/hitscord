using hitscord_net.Models.InnerModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord_net.Models.DBModels;

public abstract class ChannelDbModel
{
    public ChannelDbModel()
    {
        Id = Guid.NewGuid();
        RolesCanView = new List<RoleDbModel>();
        RolesCanWrite = new List<RoleDbModel>();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    public Guid ServerId { get; set; }

    [ForeignKey(nameof(ServerId))]
    public ServerDbModel Server { get; set; }

    public ICollection<RoleDbModel> RolesCanView { get; set; }
    public ICollection<RoleDbModel> RolesCanWrite { get; set; }
}