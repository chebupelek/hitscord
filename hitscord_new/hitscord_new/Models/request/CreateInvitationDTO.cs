using hitscord.Models.other;
using Quartz.Util;
using System.Text.RegularExpressions;

namespace hitscord.Models.response;

public class CreateInvitationDTO
{
    public required Guid ServerId { get; set; }
    public required DateTime ExpiredAt { get; set; }

    public void Validation()
    {
		var now = DateTime.Now;
		if (ExpiredAt <= now.AddMinutes(9))
		{
			throw new CustomException("Expiration time must be minimum at 10 minuts", "Create invitation", "ExpiredAt", 400, "Ссылка должна продержаться минимум 10 минут", "Валидация генерации приглашения");
		}
	}
}