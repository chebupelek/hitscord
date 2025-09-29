using hitscord.Models.other;

namespace hitscord.Models.request;

public class DeleteChannelDTO
{
    public required Guid channelId {  get; set; }

    public void Validation()
    {
        if (channelId == Guid.Empty)
        {
            throw new CustomException("Channel ID is required.", "DeleteChannel", "channelId", 400, "Необходимо указать идентификатор канала", "Валидация удаления канала");
        }
    }
}
