namespace HitscordLibrary.Models;

public class ReplyToMessageResponceDTO
{
	public required string MessageType { get; set; }
	public required Guid? ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required Guid Id { get; set; }
    public required Guid AuthorId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string Text { get; set; }
}