using HitscordLibrary.Models.other;

namespace HitscordLibrary.SocketsModels;

public class ChannelResponseSocket : NotificationObject
{
    public required bool Create { get; set; }
    public required Guid ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required ChannelTypeEnum ChannelType { get; set; }
}