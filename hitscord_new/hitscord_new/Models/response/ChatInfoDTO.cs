using hitscord.Models.db;

namespace hitscord.Models.response;

public class ChatInfoDTO
{
	public required Guid ChatId { get; set; }
	public required string ChatName { get; set; }
	public required List<UserResponseDTO> Users { get; set; }
}
