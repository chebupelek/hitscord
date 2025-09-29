using hitscord.Models.other;

namespace hitscord.Models.Sockets;

public class VoteMessageSocketDTO
{
    public required string Title { get; set; }
	public string? Content { get; set; }
	public required bool IsAnonimous { get; set; }
	public required bool Multiple { get; set; }
	public DateTime? Deadline { get; set; }
	public required List<VoteVariantMessageSocketDTO> Variants { get; set; }

	public void ValidationVote(Guid UserId)
    {
		if (Title.Length < 1 || Title.Length > 5000)
		{
			throw new CustomExceptionUser("Vote title must be between 1 and 5000 characters.", "CreateMessage", "Title", 400, "Титул голосования должен содержать от 1 до 5000 символов", "Валидация сообщения", UserId);
		}

		if (Content != null)
		{
			if (Content.Length < 1 || Content.Length > 5000)
			{
				throw new CustomExceptionUser("Vote content must be between 1 and 5000 characters.", "CreateMessage", "Content", 400, "Описание голосования должен содержать от 1 до 5000 символов", "Валидация сообщения", UserId);
			}
		}

		if (Deadline.HasValue && Deadline.Value < DateTime.UtcNow.AddMinutes(5))
		{
			throw new CustomExceptionUser("Deadline must be at least 5 minutes in the future.", "CreateMessage", "Deadline", 400, "Дедлайн должен быть как минимум через 5 минут", "Валидация сообщения", UserId);
		}

		if (Variants.Count < 2 || Variants.Count > 30)
		{
			throw new CustomExceptionUser("The number of variants must be between 2 and 30.", "CreateMessage", "Variants", 400, "Количество вариантов должно быть от 2 до 30", "Валидация сообщения", UserId);
		}

		foreach (var variant in Variants)
		{
			variant.ValidationVariant(UserId);
		}

		var expectedNumbers = Enumerable.Range(1, Variants.Count).ToList();
		var actualNumbers = Variants.Select(v => v.Number).OrderBy(n => n).ToList();

		if (!expectedNumbers.SequenceEqual(actualNumbers))
		{
			throw new CustomExceptionUser("Variant numbers must start from 1 and go sequentially without gaps.", "CreateMessage", "Variants.Number", 400, "Номера вариантов должны начинаться с 1 и идти последовательно без пропусков", "Валидация сообщения", UserId);
		}
	}
}
