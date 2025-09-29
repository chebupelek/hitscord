using hitscord.Models.other;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class ChatDbModel
{
    public ChatDbModel()
    {
        Id = Guid.NewGuid();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required string Name { get; set; }

	public ICollection<UserChatDbModel> Users { get; set; }
	public ICollection<ChatMessageDbModel> Messages { get; set; }

	public Guid? IconFileId { get; set; }
	[ForeignKey(nameof(IconFileId))]
	public FileDbModel? IconFile { get; set; }
}
