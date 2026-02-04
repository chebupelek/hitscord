using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class UserServerDbModel
{
	[Key]
    public required Guid Id { get; set; }

    [Required]
    public required Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public UserDbModel User { get; set; }

	[Required]
	public required Guid ServerId { get; set; }
	[ForeignKey(nameof(ServerId))]
	public ServerDbModel Server { get; set; }

	public Guid? InvitationId { get; set; }
	[ForeignKey(nameof(InvitationId))]
	public ServerInvitationDbModel? Invitation { get; set; }


	public required bool IsBanned { get; set; }

	public string? BanReason { get; set; }

	public DateTime? BanTime { get; set; }

    [Required]
	[MinLength(6)]
	[MaxLength(100)]
    public required string UserServerName { get; set; }

	public required bool NonNotifiable { get; set; }

	public ICollection<SubscribeRoleDbModel> SubscribeRoles { get; set; }
}