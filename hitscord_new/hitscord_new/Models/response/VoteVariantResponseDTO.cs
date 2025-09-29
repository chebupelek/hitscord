namespace hitscord.Models.response;

public class VoteVariantResponseDTO
{
	public required Guid Id { get; set; }
	public required int Number { get; set; }
	public required string Content { get; set; }
	public required int TotalVotes { get; set; }
	public required List<Guid> VotedUserIds { get; set; }
}