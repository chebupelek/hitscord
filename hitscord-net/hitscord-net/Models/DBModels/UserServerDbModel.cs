using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

public class UserServerDbModel
{
    public UserServerDbModel()
    {
        Id = Guid.NewGuid();
    }

    [Key]
    public Guid? Id { get; set; }

    public required Guid ServerId { get; set; }
    public required Guid UserId { get; set; }
    public required RoleEnum Role { get; set; }
}