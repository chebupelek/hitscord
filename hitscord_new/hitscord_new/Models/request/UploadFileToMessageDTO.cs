using HitscordLibrary.Models.other;

namespace hitscord.Models.request;

public class UploadFileToMessageDTO
{
	public required Guid ChannelId { get; set; }
	public required IFormFile File { get; set; }
}