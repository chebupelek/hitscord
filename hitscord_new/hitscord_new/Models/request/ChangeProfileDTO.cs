using HitscordLibrary.Models.other;
using System.Text.RegularExpressions;

namespace hitscord.Models.response;

public class ChangeProfileDTO
{
    public string? Name { get; set; }
    public string? Mail { get; set; }

    public void Validation()
    {
        if (!string.IsNullOrWhiteSpace(Name))
        {
            if (Name.Length < 6 || Name.Length > 50)
            {
                throw new CustomException("Account name must be between 6 and 50 characters.", "Change profile", "AccountName", 400, "Имя пользователя должно быть от 6 до 50 символов", "Валидация изменения профиля");
            }
        }

        if (!string.IsNullOrWhiteSpace(Mail))
        {
            if (Mail.Length < 6 || Mail.Length > 50)
            {
                throw new CustomException("Mail address must be between 6 and 50 characters.", "Change profile", "Mail", 400, "Почта должна быть от 6 до 50 символов", "Валидация изменения профиля");
            }
            if (!Regex.IsMatch(Mail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                throw new CustomException("Invalid mail address format.", "Change profile", "Mail", 400, "Неверный формат почты", "Валидация изменения профиля");
            }
        }
    }
}