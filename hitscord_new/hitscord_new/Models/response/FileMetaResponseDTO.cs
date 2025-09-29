namespace hitscord.Models.response;

public class FileMetaResponseDTO
{
	public required Guid FileId { get; set; }
	public required string FileName { get; set; }
	public required string FileType { get; set; }
	public required long FileSize { get; set; }
}