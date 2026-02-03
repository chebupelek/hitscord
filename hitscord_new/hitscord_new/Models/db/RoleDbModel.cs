using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class RoleDbModel
{
    public RoleDbModel()
    {
        Id = Guid.NewGuid();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    public required RoleEnum Role { get; set; }

    [Required]
    public required Guid ServerId { get; set; }

    [ForeignKey(nameof(ServerId))]
    public ServerDbModel Server { get; set; }

    public required string Color { get; set; }

    public required string Tag { get; set; }

    public required bool ServerCanChangeRole { get; set; }

    public required bool ServerCanWorkChannels { get; set; }

    public required bool ServerCanDeleteUsers { get; set; }

    public required bool ServerCanMuteOther { get; set; }

    public required bool ServerCanDeleteOthersMessages { get; set; }

    public required bool ServerCanIgnoreMaxCount { get; set; }

    public required bool ServerCanCreateRoles { get; set; }

    public required bool ServerCanCreateLessons { get; set; }

    public required bool ServerCanCheckAttendance { get; set; }

	public required bool ServerCanUseInvitations { get; set; }

	public ICollection<ChannelCanSeeDbModel> ChannelCanSee { get; set; }
    public ICollection<ChannelCanWriteDbModel> ChannelCanWrite { get; set; }
    public ICollection<ChannelCanWriteSubDbModel> ChannelCanWriteSub { get; set; }
    public ICollection<ChannelNotificatedDbModel> ChannelNotificated { get; set; }
    public ICollection<ChannelCanUseDbModel> ChannelCanUse { get; set; }
    public ICollection<ChannelCanJoinDbModel> ChannelCanJoin { get; set; }
}
