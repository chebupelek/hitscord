using hitscord.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.request;

public class ChangePasswordDTO
{
    public required Guid UserId { get; set; }
    public required string Password { get; set; }

    public void Validation()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new CustomException("Password is required.", "Account", "Password", 400, "Необходимо отправить пароль", "Валидация регистрации");
        }
        if (Password.Length < 6)
        {
            throw new CustomException("Password must have at least 6 characters.", "Account", "Password", 400, "Пароль должен быть больше 6 символов", "Валидация регистрации");
        }
	}
}
