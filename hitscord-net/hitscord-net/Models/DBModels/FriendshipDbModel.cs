using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

public class FriendshipDbModel
{
    public FriendshipDbModel()
    {
        CreateTime = DateTime.UtcNow;
    }

    [Required]
    public Guid? UserFirstId { get; set; }

    [ForeignKey(nameof(UserFirstId))]
    public UserDbModel? UserFirst { get; set; }

    [Required]
    public Guid? UserSecondId { get; set; }

    [ForeignKey(nameof(UserSecondId))]
    public UserDbModel? UserSecond { get; set; }

    public DateTime? CreateTime { get; set; }
}