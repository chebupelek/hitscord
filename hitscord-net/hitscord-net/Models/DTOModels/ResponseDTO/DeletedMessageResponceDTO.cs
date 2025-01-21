namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class DeletedMessageResponceDTO
{
    public required Guid ServerId { get; set; }
    public required Guid ChannelId { get; set; }
    public required Guid MessageId { get; set; }
}