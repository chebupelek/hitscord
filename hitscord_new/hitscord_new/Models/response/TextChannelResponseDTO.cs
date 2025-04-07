namespace hitscord.Models.response;

public class TextChannelResponseDTO
{
    public required Guid ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required bool CanWrite { get; set; }
}