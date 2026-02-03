using hitscord.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.request;

public class SubscribeDTO
{
    public required string InvitationToken { get; set; }
    public string? UserName { get; set; }

    public void Validation()
    {
        if (!string.IsNullOrWhiteSpace(UserName))
        {
            if (UserName.Length < 6 || UserName.Length > 50)
            {
                throw new CustomException(
                    "UserName must be between 6 and 50 characters.",
                    "Subscribe",
                    "UserName",
                    400,
                    "Имя пользователя должно содержать от 6 до 50 символов",
                    "Валидация подписки"
                );
            }

            if (!Regex.IsMatch(UserName, @"^[a-zA-Z0-9а-яА-ЯёЁ\s-]+$"))
            {
                throw new CustomException(
                    "UserName contains invalid characters.",
                    "Subscribe",
                    "UserName",
                    400,
                    "Имя пользователя содержит недопустимые символы",
                    "Валидация подписки"
                );
            }
        }
    }
}