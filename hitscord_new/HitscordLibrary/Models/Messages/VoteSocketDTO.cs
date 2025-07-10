namespace HitscordLibrary.Models.Messages;
public class VoteSocketDTO
{
    public required string Token { get; set; }
    public required Guid VoteId { get; set; }
}