namespace hitscord.Models.response;

public class ChangeStreamStatus
{
    public required Guid ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required Guid UserId { get; set; }
    public required bool IsStream { get; set; }
}