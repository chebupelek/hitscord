using HitscordLibrary.Models.other;

namespace hitscord.Models.request;

public class UnsubscribeForCreatorDTO
{
    public required Guid serverId {  get; set; }
    public required Guid newCreatorId { get; set; }

    public void Validate()
    {
        if (serverId == Guid.Empty)
        {
            throw new CustomException(
                    "Requset must have serverId",
                    "Unsubscribe for creator",
                    "serverId",
                    400,
                    "Необходимо отправить serverID",
                    "Валидация отписки для создателя"
                );
        }

        if (newCreatorId == Guid.Empty)
        {
            throw new CustomException(
                    "Requset must have newCreatorId",
                    "Unsubscribe for creator",
                    "newCreatorId",
                    400,
                    "Необходимо отправить newCreatorId",
                    "Валидация отписки для создателя"
                );
        }
    }
}