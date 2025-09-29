using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.Sockets;

public class UpdateMessageSocketDTO
{
    public required string Token { get; set; }
    public required long MessageId { get; set; }
    [Required]
    [MinLength(1)]
    [MaxLength(5000)]
    public required string Text { get; set; }
    public required Guid ChannelId { get; set; }
}
