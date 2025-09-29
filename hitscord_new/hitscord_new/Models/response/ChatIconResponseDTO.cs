namespace hitscord.Models.response;

public class ChatIconResponseDTO
{
    public required Guid ChatId { get; set; }
    public required FileMetaResponseDTO Icon { get; set; }
}