using hitscord.Models.other;

namespace hitscord.Models.response;

public class ChangeMaxCountRequestDTO
{
    public required Guid VoiceChannelId { get; set; }
    public required int MaxCount { get; set; }

    public void Validation()
    {
		if (MaxCount < 2 || MaxCount > 999)
		{
			throw new CustomException("Max count mast be between 2 and 999.", "ChangeMaxCount", "Max count", 400, "Максимальное количество должно быть между 2 и 999", "Изменение максимального количества");
		}
	}
}