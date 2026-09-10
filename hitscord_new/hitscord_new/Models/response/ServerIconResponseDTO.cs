namespace hitscord.Models.response;

public class ServerIconResponseDTO
{
    public required Guid ServerId { get; set; }
    public required FileMetaResponseDTO Icon { get; set; }
}