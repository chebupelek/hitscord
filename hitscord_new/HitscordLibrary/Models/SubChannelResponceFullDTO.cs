namespace HitscordLibrary.Models;

public class SubChannelResponceFullDTO
{
    public required Guid SubChannelId { get; set; }
    public required List<Guid> RolesCanUse { get; set; }
    public required bool IsNotifiable { get; set; }
}