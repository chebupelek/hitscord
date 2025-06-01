using HitscordLibrary.Models.other;

namespace HitscordLibrary.Models.Messages;

public class FileForWebsocketDTO
{
	public required string FileName { get; set; }
	public required string ContentType { get; set; }
	public required string Base64Content { get; set; }
}
