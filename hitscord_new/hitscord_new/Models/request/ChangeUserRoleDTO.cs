using hitscord.Models.other;

namespace hitscord.Models.request;

public class ChangeUserRoleDTO
{
    public required Guid ServerId { get; set; }
    public required Guid UserId { get; set; }
    public required Guid Role { get; set; }

    public void Validation()
    {
        if (ServerId == Guid.Empty)
        {
            throw new CustomException(
                "ServerId is required.",
                "ChangeUserRole",
                "ServerId",
                400,
                "Необходимо отправить идентификатор сервера",
                "Валидация изменения роли пользователя"
            );
        }

        if (UserId == Guid.Empty)
        {
            throw new CustomException(
                "UserId is required.",
                "ChangeUserRole",
                "UserId",
                400,
                "Необходимо отправить идентификатор пользователя",
                "Валидация изменения роли пользователя"
            );
        }

        if (Role == Guid.Empty)
        {
            throw new CustomException(
                "Role is required.",
                "ChangeUserRole",
                "Role",
                400,
                "Необходимо отправить идентификатор роли",
                "Валидация изменения роли пользователя"
            );
        }
    }
}