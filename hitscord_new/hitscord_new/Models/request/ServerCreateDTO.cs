using HitscordLibrary.Models.other;

namespace hitscord.Models.request;

public class ServerCreateDTO
{
    public required string Name { get; set; }

    public void Validation()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new CustomException("Name is required.", "Server", "Name", 400, "Необходимо отправить название сервера", "Валидация создания сервера");
        }
        if (Name.Length < 1 || Name.Length > 50)
        {
            throw new CustomException("Name must be between 1 and 50 characters.", "Server", "Name", 400, "Название сервера должно быть от 1 до 50 символов", "Валидация создания сервера");
        }
    }
}