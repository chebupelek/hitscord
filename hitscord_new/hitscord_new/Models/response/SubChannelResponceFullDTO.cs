namespace hitscord.Models.response;
public class SubChannelResponceFullDTO
{
    public required Guid SubChannelId { get; set; }
    public required List<Guid> RolesCanUse { get; set; }
}