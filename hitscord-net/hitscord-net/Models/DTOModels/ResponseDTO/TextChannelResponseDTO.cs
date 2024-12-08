using Org.BouncyCastle.Bcpg;

namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class TextChannelResponseDTO
{
    public required Guid ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required bool CanWrite { get; set; }
}