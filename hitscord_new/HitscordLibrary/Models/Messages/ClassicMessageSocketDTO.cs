using HitscordLibrary.Models.other;
using System.Xml.Linq;

namespace HitscordLibrary.Models.Messages;

public class ClassicMessageSocketDTO
{
    public required string Text { get; set; }
    public required bool NestedChannel { get; set; }
	public List<Guid>? Files { get; set; }

	public void ValidationClassic(Guid UserId)
    {

        if (string.IsNullOrWhiteSpace(Text))
        {
            throw new CustomExceptionUser("Message text is required.", "CreateMessage", "Text", 400, "Текст сообщения обязателен.", "Валидация сообщения", UserId);
		}

        if (Text.Length < 1 || Text.Length > 5000)
        {
			throw new CustomExceptionUser("Message text must be between 1 and 5000 characters.", "CreateMessage", "Text", 400, "Текст сообщения должен содержать от 1 до 5000 символов", "Валидация сообщения", UserId);
		}

		if (Files != null)
        {
            if (Files.Count() > 10 && Files.Count() < 1)
            {
				throw new CustomExceptionUser("" +
                    "files count must be between 1 and 10", "CreateMessage", "Text", 400, "Файлов должно быть от 1 до 10", "Валидация сообщения", UserId);
			}
        }
    }
}
