using hitscord.Models.other;

namespace hitscord.Models.request;

public class UnsubscribeDTO
{
    public required Guid serverId {  get; set; }

    public void Validate()
    {
        if (serverId == Guid.Empty)
        {
            throw new CustomException(
                    "Requset must have serverId",
                    "Unsubscribe",
                    "serverId",
                    400,
                    "Необходимо отправить serverID",
                    "Валидация отписки"
                );
        }
    }
}