using hitscord_net.Models.InnerModels;
using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

public class ServerDbModel
{
    public ServerDbModel()
    {
        Id = Guid.NewGuid();
        Channels = new List<ChannelDbModel>();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public required string Name { get; set; }

    public required UserDbModel Admin {  get; set; }

    public List<ChannelDbModel>? Channels { get; set; }
}