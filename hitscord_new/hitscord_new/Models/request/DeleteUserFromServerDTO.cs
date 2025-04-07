using HitscordLibrary.Models.other;

namespace hitscord.Models.DTOModels.request;

public class DeleteUserFromServerDTO
{
    public required Guid ServerId { get; set; }
    public required Guid UserId { get; set; }

    public void Validation()
    {
        if (ServerId == Guid.Empty)
        {
            throw new CustomException(
                "ServerId is required.",
                "DeleteUserFromServer",
                "ServerId",
                400,
                "Необходимо отправить идентификатор сервера",
                "Валидация удаления пользователя с сервера"
            );
        }

        if (UserId == Guid.Empty)
        {
            throw new CustomException(
                "UserId is required.",
                "DeleteUserFromServer",
                "UserId",
                400,
                "Необходимо отправить идентификатор пользователя",
                "Валидация удаления пользователя с сервера"
            );
        }
    }
}