using HitscordLibrary.Models.other;

namespace hitscord.Models.request;

public class CreateChannelDTO
{
    public required Guid ServerId {  get; set; }
    public required string Name { get; set; }
    public required ChannelTypeEnum ChannelType { get; set; }

    public void Validation()
    {
        if (ServerId == Guid.Empty)
        {
            throw new CustomException("ServerId is required.", "CreateChannel", "ServerId", 400, "Необходимо указать ServerId", "Валидация канала");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new CustomException("Channel name is required.", "CreateChannel", "Name", 400, "Необходимо указать название канала", "Валидация канала");
        }

        if (Name.Length < 1 || Name.Length > 100)
        {
            throw new CustomException("Channel name must be between 1 and 100 characters.", "CreateChannel", "Name", 400, "Название канала должно содержать от 1 до 100 символов", "Валидация канала");
        }
    }
}
