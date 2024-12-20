using hitscord_net.Models.InnerModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord_net.Models.DBModels;

public class ServerDbModel
{
    public ServerDbModel()
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
    public required Guid CreatorId { get; set; }

    [ForeignKey(nameof(CreatorId))]
    public UserDbModel Creator { get; set; }

    public ICollection<UserServerDbModel> UserServer { get; set; }
    public ICollection<ChannelDbModel> Channels { get; set; }
    public ICollection<RoleDbModel> RolesCanDeleteUsers { get; set; }
    public ICollection<RoleDbModel> RolesCanWorkWithChannels { get; set; }
    public ICollection<RoleDbModel> RolesCanChangeRolesUsers { get; set; }
}