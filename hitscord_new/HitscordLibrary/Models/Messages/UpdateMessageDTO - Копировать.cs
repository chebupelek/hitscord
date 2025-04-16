using System.ComponentModel.DataAnnotations;

namespace HitscordLibrary.Models.Messages;

public class UpdateMessageSocketDTO
{
    public required string Token { get; set; }
    public required Guid MessageId { get; set; }
    [Required]
    [MinLength(1)]
    [MaxLength(5000)]
    public required string Text { get; set; }

    public List<Guid>? Roles { get; set; }
    public List<Guid>? UserIds { get; set; }
}
