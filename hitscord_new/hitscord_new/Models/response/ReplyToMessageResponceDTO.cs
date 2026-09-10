namespace hitscord.Models.response;

public class ReplyToMessageResponceDTO
{
	public required string MessageType { get; set; }
	public required Guid? ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required long Id { get; set; }
    public Guid? AuthorId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string Text { get; set; }
}