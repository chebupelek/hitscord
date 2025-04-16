namespace HitscordLibrary.Models.Messages;
public class DeleteMessageSocketDTO
{
    public required string Token { get; set; }
    public required Guid messageId { get; set; }
}