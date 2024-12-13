using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord_net.Models.DBModels;

public class UserServerDbModel
{
    [Required]
    public required Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserDbModel User { get; set; }

    [Required]
    public required Guid ServerId { get; set; }

    [ForeignKey(nameof(ServerId))]
    public ServerDbModel Server { get; set; }

    [Required]
    public required Guid RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public RoleDbModel Role { get; set; }

    [Required]
    [MaxLength(100)]
    public required string UserServerName { get; set; }

}