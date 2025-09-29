namespace hitscord.Models.response;

public class UnsubscribeResponseDTO
{
    public required Guid UserId { get; set; }
    public required Guid ServerId { get; set; }
}