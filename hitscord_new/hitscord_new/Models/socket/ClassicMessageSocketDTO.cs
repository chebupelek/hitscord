using hitscord.Models.other;

namespace hitscord.Models.Sockets;
public class ClassicMessageSocketDTO
{
    public string? Text { get; set; }
    public required bool NestedChannel { get; set; }
	public List<Guid>? Files { get; set; }

	public void ValidationClassic()
    {

        if(string.IsNullOrWhiteSpace(Text) && (Files == null || Files.Count() < 1))
        {
            throw new CustomException("Message text is required.", "CreateMessage", "Text", 400, "Текст сообщения обязателен.", "Валидация сообщения");
		}

        if (Text != null)
        {
			if (Text.Length < 1 || Text.Length > 5000)
			{
				throw new CustomException("Message text must be between 1 and 5000 characters.", "CreateMessage", "Text", 400, "Текст сообщения должен содержать от 1 до 5000 символов", "Валидация сообщения");
			}
		}

		if (Files != null)
        {
            if (Files.Count() > 10)
            {
				throw new CustomException("" +
                    "files count must be between 1 and 10", "CreateMessage", "Text", 400, "Файлов должно быть от 1 до 10", "Валидация сообщения");
			}
        }
    }
}
