using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Message.Models.DB;

public class MessageDbModel
{
    public MessageDbModel()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(5000)]
    public required string Text { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public required Guid UserId { get; set; }
    public required Guid TextChannelId { get; set; }
    public Guid? NestedChannelId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    [ForeignKey(nameof(ReplyToMessageId))]
    public MessageDbModel? ReplyToMessage { get; set; }
    public DateTime? DeleteTime { get; set; }
}