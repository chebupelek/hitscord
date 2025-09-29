namespace hitscord.Models.response;

public class MessageListResponseDTO
{
    public required List<object> Messages { get; set; }
	public required int NumberOfMessages { get; set; }
    public required long StartMessageId { get; set;}
	public required int RemainingMessagesCount { get; set; }
	public required int AllMessagesCount { get; set; }
}