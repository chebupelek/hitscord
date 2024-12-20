namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class MessageResponceDTO
{
    public required Guid Id { get; set; }   
    public required string Text { get; set; }
    public required Guid AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}