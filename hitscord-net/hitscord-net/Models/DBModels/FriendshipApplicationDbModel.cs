using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord_net.Models.DBModels;

public class FriendshipApplicationDbModel
{
    public FriendshipApplicationDbModel()
    {
        CreateTime = DateTime.UtcNow;
    }

    [Required]
    public Guid? UserFromId { get; set; }

    [ForeignKey(nameof(UserFromId))]
    public UserDbModel? UserFrom { get; set; }

    [Required]
    public Guid? UserToId { get; set; }

    [ForeignKey(nameof(UserToId))]
    public UserDbModel? UserTo { get; set; }

    
    public DateTime? CreateTime { get; set; }
}