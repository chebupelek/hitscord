using hitscord.Models.other;

namespace hitscord.Models.response;

public class ChangeSelfMutedStatus
{
    public required Guid ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required Guid UserId { get; set; }
    public required MuteStatusEnum MuteStatus { get; set; }
}