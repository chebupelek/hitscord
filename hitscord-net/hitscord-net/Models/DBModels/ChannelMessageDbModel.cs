using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DBModels;

public class ChannelMessageDbModel : MessageDbModel
{
    [Required]
    public required Guid NestedChannelId { get; set; }

    [ForeignKey(nameof(NestedChannelId))]
    public TextChannelDbModel NestedChannel { get; set; }
}