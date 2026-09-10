namespace hitscord.Models.response;

public class LastReadMessageDTO
{
	public object? Message { get; set; }
	public UserChatResponseDTO? Author { get; set; }
}