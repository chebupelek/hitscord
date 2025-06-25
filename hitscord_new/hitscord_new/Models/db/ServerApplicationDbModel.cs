using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class ServerApplicationDbModel
{
    public ServerApplicationDbModel()
    {
        Id = Guid.NewGuid();
    }

    [Key]
    public Guid Id { get; set; }

	[Required]
	public Guid UserId { get; set; }
	[ForeignKey(nameof(UserId))]
	public UserDbModel User { get; set; }

	[Required]
	public Guid ServerId { get; set; }
	[ForeignKey(nameof(ServerId))]
	public ServerDbModel Server { get; set; }

	public string? ServerUserName { get; set; }
	public required DateTime CreatedAt { get; set; }
}