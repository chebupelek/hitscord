namespace HitscordLibrary.SocketsModels;

public class UserVoiceChannelResponseDTO : NotificationObject
{
    public required Guid ServerId { get; set; }
    public required bool isEnter {  get; set; }
    public required Guid UserId { get; set; }
    public required Guid ChannelId { get; set; }
}