using hitscord.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.request;

public class AdminLoginDTO
{
    public required string Login { get; set; }
    public required string Password { get; set; }

    public void Validation()
    {
        if (string.IsNullOrWhiteSpace(Login))
        {
            throw new CustomException("Login address is required.", "Login", "Login", 400, "Необходимо отправить логин", "Валидация логина");
        }
        if (Login.Length < 6 || Login.Length > 50)
        {
            throw new CustomException("Login must be between 6 and 50 characters.", "Login", "Login", 400, "Логин должен быть от 6 до 50 символов", "Валидация логина");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new CustomException("Password is required.", "Login", "Password", 400, "Необходимо отправить пароль", "Валидация логина");
        }
        if (Password.Length < 6)
        {
            throw new CustomException("Password must have at least 6 characters.", "Login", "Password", 400, "Пароль должен быть больше 6 символов", "Валидация логина");
        }
    }
}