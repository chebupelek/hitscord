namespace hitscord.Models.response;

public class DeletedMessageResponceDTO
{
    public required Guid ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required long MessageId { get; set; }
}