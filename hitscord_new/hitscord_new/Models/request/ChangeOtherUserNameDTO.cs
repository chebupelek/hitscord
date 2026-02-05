using hitscord.Models.other;

namespace hitscord.Models.request;

public class ChangeOtherUserNameDTO
{
	public required Guid ServerId { get; set; }
	public required Guid UserId { get; set; }
	public required string Name { get; set; }

	public void Validation()
	{
		if (string.IsNullOrWhiteSpace(Name))
		{
			throw new CustomException("Name is required.", "Change name", "Name", 400, "Необходимо отправить имя", "Валидация изменении имени");
		}
		if (Name.Length < 6 || Name.Length > 50)
		{
			throw new CustomException("Name must be between 6 and 50 characters.", "Change name", "Name", 400, "Имя должно быть от 6 до 50 символов", "Валидация изменении имени");
		}
	}

}