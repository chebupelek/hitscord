namespace hitscord.Models.Sockets;
public class VoteVariantSocketDTO
{
    public required string Token { get; set; }
    public required Guid VoteVariantId { get; set; }
    public required bool isChannel { get; set; }
}