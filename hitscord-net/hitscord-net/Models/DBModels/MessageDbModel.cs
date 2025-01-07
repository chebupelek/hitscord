using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

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

    public required ICollection<RoleDbModel> Roles { get; set; }
    public required IList<string> Tags { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public required Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserDbModel User { get; set; }

    [Required]
    public required Guid TextChannelId { get; set; }

    [ForeignKey(nameof(TextChannelId))]
    public TextChannelDbModel TextChannel { get; set; }

    public Guid? NestedChannelId { get; set; }
    [ForeignKey(nameof(NestedChannelId))]
    public TextChannelDbModel? NestedChannel { get; set; }

    public Guid? ReplyToMessageId { get; set; }
    [ForeignKey(nameof(ReplyToMessageId))]
    public MessageDbModel? ReplyToMessage { get; set; }
}