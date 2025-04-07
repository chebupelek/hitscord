using System.ComponentModel.DataAnnotations;

namespace Message.Models.request;

public class UpdateMessageDTO
{
    public required Guid MessageId { get; set; }
    [Required]
    [MinLength(1)]
    [MaxLength(5000)]
    public required string Text { get; set; }

    public List<Guid>? Roles { get; set; }
    public List<Guid>? UserIds { get; set; }
}
