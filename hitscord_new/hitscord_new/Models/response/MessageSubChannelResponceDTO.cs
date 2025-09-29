namespace hitscord.Models.response;

public class MessageSubChannelResponceDTO
{
    public required Guid SubChannelId { get; set; }
    public required bool CanUse { get; set; }
    public required bool IsNotifiable { get; set; }
}