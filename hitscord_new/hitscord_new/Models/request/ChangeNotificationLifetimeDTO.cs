using hitscord.Models.other;

namespace hitscord.Models.response;

public class ChangeNotificationLifetimeDTO
{
    public required int Lifetime { get; set; }

    public void Validation()
    {
		if ((Lifetime < 2) || (Lifetime > 20))
		{
			throw new CustomException("Lifetime must be between 2 and 20.", "Profile", "Lifetime", 400, "Время жизни уведомления должно быть от 2 до 20", "Валидация изменения времени жизни уведмления");
		}
    }
}