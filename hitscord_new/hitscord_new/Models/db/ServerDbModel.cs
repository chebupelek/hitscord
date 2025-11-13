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
    [MinLength(6)]
    [MaxLength(50)]
    public required string Name { get; set; }
    public ICollection<RoleDbModel> Roles { get; set; }
    public ICollection<ChannelDbModel> Channels { get; set; }
	public ICollection<UserServerDbModel> Subscribtions { get; set; }
	public Guid? IconFileId { get; set; }
	[ForeignKey(nameof(IconFileId))]
	public FileDbModel? IconFile { get; set; }
	public required bool IsClosed { get; set; }
    public required ServerTypeEnum ServerType { get; set; }
}