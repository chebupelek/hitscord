using hitscord.Models.other;

namespace hitscord.Models.request;

public class ChatNameRequestDTO
{
    public required Guid ChatId { get; set; }
    public required string Name { get; set; }

	public void Validation()
	{
		if (string.IsNullOrWhiteSpace(Name))
		{
			throw new CustomException("Name is required.", "Change chat name", "Name", 400, "Необходимо отправить имя", "Валидация изменении имени чата");
		}
		if (Name.Length < 1 || Name.Length > 50)
		{
			throw new CustomException("Name must be between 1 and 50 characters.", "Change chat name", "Name", 400, "Имя должно быть от 1 до 50 символов", "Валидация изменении имени чата");
		}
	}
}