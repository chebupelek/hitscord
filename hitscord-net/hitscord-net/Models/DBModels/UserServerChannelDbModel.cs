using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace hitscord_net.Models.DBModels;

public class UserServerChannelDbModel
{
    public UserServerChannelDbModel()
    {
        Id = Guid.NewGuid();
    }
    [Key]
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public Guid? ServerId { get; set; }
    public Guid? ChannelId { get; set;}
}