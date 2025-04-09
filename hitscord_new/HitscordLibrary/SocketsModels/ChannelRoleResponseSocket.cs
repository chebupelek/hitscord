using HitscordLibrary.Models.other;

namespace HitscordLibrary.SocketsModels;

public class ChannelRoleResponseSocket : NotificationObject
{
    public required Guid ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required bool Add { get; set; }
    public required ChangeRoleTypeEnum Type { get; set; }
    public required Guid RoleId { get; set; }
}