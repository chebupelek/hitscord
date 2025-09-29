using hitscord.Models.other;

namespace hitscord.Models.Sockets;

public class VoteVariantMessageSocketDTO
{
    public required int Number { get; set; }
	public required string Content { get; set; }

	public void ValidationVariant(Guid UserId)
	{
		if (Number < 1)
		{
			throw new CustomExceptionUser("Variant number must be at least 1.", "CreateMessage", "Variant.Number", 400, "Номер варианта должен быть не меньше 1", "Валидация варианта", UserId);
		}

		if (Content.Length < 1 || Content.Length > 5000)
		{
			throw new CustomExceptionUser("Variant content must be between 1 and 5000 characters.", "CreateMessage", "Variant.Content", 400, "Содержимое варианта должно быть от 1 до 5000 символов", "Валидация варианта", UserId);
		}
	}
}
