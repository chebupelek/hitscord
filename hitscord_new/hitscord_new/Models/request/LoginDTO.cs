using HitscordLibrary.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.request;

public class LoginDTO
{
    public required string Mail { get; set; }
    public required string Password { get; set; }

    public void Validation()
    {
        if (string.IsNullOrWhiteSpace(Mail))
        {
            throw new CustomException("Mail address is required.", "Login", "Mail", 400, "Необходимо отправить почту", "Валидация логина");
        }
        if (Mail.Length < 1 || Mail.Length > 50)
        {
            throw new CustomException("Mail address must be between 1 and 50 characters.", "Login", "Mail", 400, "Почта должна быть от 1 до 50 символов", "Валидация логина");
        }
        if (!Regex.IsMatch(Mail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
        {
            throw new CustomException("Invalid mail address format.", "Login", "Mail", 400, "Неверный формат почты", "Валидация логина");
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