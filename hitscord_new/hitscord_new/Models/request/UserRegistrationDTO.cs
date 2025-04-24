using HitscordLibrary.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.request;

public class UserRegistrationDTO
{
    public required string Mail { get; set; }
    public required string Password { get; set; }
    public required string AccountName { get; set; }

    public void Validation()
    {
        if (string.IsNullOrWhiteSpace(Mail))
        {
            throw new CustomException("Mail address is required.", "Account", "Mail", 400, "Необходимо отправить почту", "Валидация регистрации");
        }
        if (Mail.Length < 6 || Mail.Length > 50)
        {
            throw new CustomException("Mail address must be between 6 and 50 characters.", "Account", "Mail", 400, "Почта должна быть от 6 до 50 символов", "Валидация регистрации");
        }
        if (!Regex.IsMatch(Mail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
        {
            throw new CustomException("Invalid mail address format.", "Account", "Mail", 400, "Неверный формат почты", "Валидация регистрации");
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
        if (AccountName.Length < 1 || AccountName.Length > 50)
        {
            throw new CustomException("Account name must be between 1 and 50 characters.", "Account", "AccountName", 400, "Имя пользователя должно быть от 1 до 50 символов", "Валидация регистрации");
        }
    }
}
