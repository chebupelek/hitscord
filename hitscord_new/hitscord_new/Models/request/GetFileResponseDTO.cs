namespace hitscord.Models.request;

public class GetFileResponseDTO
{
    public required Guid ChannelId { get; set; }
	public required Guid FileId { get; set; }
}