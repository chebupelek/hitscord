using HitscordLibrary.Models.other;
using System.ComponentModel.DataAnnotations;

namespace Message.Models.request;

public class CreateMessageDTO
{
    public required Guid ChannelId { get; set; }
    public required string Text { get; set; }

    public List<Guid>? Roles { get; set; }
    public List<Guid>? UserIds { get; set; }
    public required bool NestedChannel { get; set; }
    public Guid? ReplyToMessageId { get; set; }

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
    }
}
