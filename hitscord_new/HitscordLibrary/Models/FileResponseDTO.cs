namespace HitscordLibrary.Models;

public class FileResponseDTO
{
	public required Guid FileId { get; set; }
	public required string FileName { get; set; }
	public required string FileType { get; set; }
	public string? Base64File { get; set; }
}