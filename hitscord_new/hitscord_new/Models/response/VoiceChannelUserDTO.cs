using HitscordLibrary.Models.other;

namespace hitscord.Models.response;

public class VoiceChannelUserDTO
{
    public required Guid UserId { get; set; }
    public required MuteStatusEnum MuteStatus { get; set; }
}
