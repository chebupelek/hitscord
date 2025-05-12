namespace HitscordLibrary.Models;

public class MessageSubChannelResponceDTO
{
    public required Guid SubChannelId { get; set; }
    public required bool CanUse { get; set; }
}