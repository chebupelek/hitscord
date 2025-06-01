using HitscordLibrary.Models;

namespace hitscord.Models.response;

public class ServerIconResponseDTO
{
    public required Guid ServerId { get; set; }
    public required FileResponseDTO Icon { get; set; }
}