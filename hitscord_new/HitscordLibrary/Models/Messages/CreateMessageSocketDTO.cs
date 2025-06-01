using HitscordLibrary.Models.other;

namespace HitscordLibrary.Models.Messages;

public class CreateMessageSocketDTO
{
    public required string Token { get; set; }
    public required Guid ChannelId { get; set; }
    public required string Text { get; set; }
    public required bool NestedChannel { get; set; }
    public Guid? ReplyToMessageId { get; set; }
	public List<FileForWebsocketDTO>? Files { get; set; }

	public void Validation()
    {
        if (ChannelId == Guid.Empty)
        {
            throw new CustomException("ChannelId cannot be empty.", "CreateMessage", "ChannelId", 400, "ChannelId не может быть пустым.", "Валидация сообщения");
        }

        if (string.IsNullOrWhiteSpace(Text))
        {
            throw new CustomException("Message text is required.", "CreateMessage", "Text", 400, "Текст сообщения обязателен.", "Валидация сообщения");
        }

        if (Files != null)
        {
            if (Files.Count() > 10 && Files.Count() < 1)
            {
				throw new CustomException("" +
                    "files count must be between 1 and 10", "CreateMessage", "Text", 400, "Файлов должно быть от 1 до 10", "Валидация сообщения");
			}
        }
    }
}
