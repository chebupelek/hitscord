using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Message.Models.DB;

public class ClassicMessageDbModel : MessageDbModel
{
    [Required]
    [MinLength(1)]
    [MaxLength(5000)]
    public required string Text { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? NestedChannelId { get; set; }
    public List<Guid>? FilesId { get; set; }
}