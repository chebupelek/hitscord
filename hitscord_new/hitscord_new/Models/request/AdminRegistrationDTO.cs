using hitscord.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.request;

public class AdminRegistrationDTO
{
    public required string Login { get; set; }
    public required string Password { get; set; }
    public required string AccountName { get; set; }

    public void Validation()
    {
        if (string.IsNullOrWhiteSpace(Login))
        {
            throw new CustomException("Login address is required.", "Account", "Login", 400, "Необходимо отправить логин", "Валидация регистрации");
        }
        if (Login.Length < 10 || Login.Length > 50)
        {
            throw new CustomException("Login address must be between 10 and 50 characters.", "Account", "Login", 400, "Логин должен быть от 10 до 50 символов", "Валидация регистрации");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new CustomException("Password is required.", "Account", "Password", 400, "Необходимо отправить пароль", "Валидация регистрации");
        }
        if (Password.Length < 6)
        {
            throw new CustomException("Password must have at least 6 characters.", "Account", "Password", 400, "Пароль должен быть больше 6 символов", "Валидация регистрации");
        }

        if (string.IsNullOrWhiteSpace(AccountName))
        {
            throw new CustomException("Account name is required.", "Account", "AccountName", 400, "Необходимо отправить имя пользователя", "Валидация регистрации");
        }
        if (AccountName.Length < 6 || AccountName.Length > 50)
        {
            throw new CustomException("Account name must be between 6 and 50 characters.", "Account", "AccountName", 400, "Имя пользователя должно быть от 6 до 50 символов", "Валидация регистрации");
        }
		if (!Regex.IsMatch(AccountName, @"^[a-zA-Zа-яА-ЯёЁ0-9 ]+$"))
		{
			throw new CustomException("Account name must contain only letters and digits.", "Account", "AccountName", 400, "Имя пользователя должно содержать только русские или английские буквы и цифры", "Валидация регистрации");
		}
	}
}
