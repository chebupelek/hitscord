using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

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
    public ICollection<RoleDbModel> Roles { get; set; }
    public ICollection<ChannelDbModel> Channels { get; set; }
    public Guid? IconId { get; set; }
    public required bool IsClosed { get; set; }
}