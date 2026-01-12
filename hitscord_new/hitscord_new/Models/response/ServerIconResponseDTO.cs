namespace hitscord.Models.response;

public class ServerIconResponseDTO
{
    public required Guid ServerId { get; set; }
    public FileMetaResponseDTO? Icon { get; set; }
}