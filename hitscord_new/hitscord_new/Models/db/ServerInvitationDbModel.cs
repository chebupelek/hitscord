using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class ServerInvitationDbModel
{
    public ServerInvitationDbModel()
    {
        Id = Guid.NewGuid();
    }

    [Key]
    public Guid Id { get; set; }

	[Required]
	public required Guid ServerId { get; set; }
	[ForeignKey(nameof(ServerId))]
	public ServerDbModel Server { get; set; }

	public Guid? UserId { get; set; }
	[ForeignKey(nameof(UserId))]
	public UserDbModel? User { get; set; }

	public required string Token { get; set; }
	public required DateTime ExpiresAt { get; set; }
	public required bool IsRevoked { get; set; }
}