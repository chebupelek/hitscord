using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

public class ReplyMessageDbModel : MessageDbModel
{
    [Required]
    public required Guid ReplyToMessageId { get; set; }

    [ForeignKey(nameof(ReplyToMessageId))]
    public MessageDbModel ReplyToMessage { get; set; }
}