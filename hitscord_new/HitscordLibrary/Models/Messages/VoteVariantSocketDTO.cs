namespace HitscordLibrary.Models.Messages;
public class VoteVariantSocketDTO
{
    public required string Token { get; set; }
    public required Guid VoteVariantId { get; set; }
}