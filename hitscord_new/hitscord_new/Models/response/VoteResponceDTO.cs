namespace hitscord.Models.response;

public class VoteResponceDTO : MessageResponceDTO
{
	public required string Title { get; set; }
	public string? Content { get; set; }
	public required bool IsAnonimous { get; set; }
	public required bool Multiple { get; set; }
	public DateTime? Deadline { get; set; }
	public required int TotalUsers { get; set; }
	public required List<VoteVariantResponseDTO> Variants { get; set; }
}