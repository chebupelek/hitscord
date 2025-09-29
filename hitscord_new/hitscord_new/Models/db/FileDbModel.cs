using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hitscord.Models.db;

public class FileDbModel
{
    [Key]
    public required Guid Id { get; set; }
    public required string Path { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
	public required long Size { get; set; }
	public required Guid Creator { get; set; }
	public required bool IsApproved { get; set; }
	public required DateTime CreatedAt { get; set; }

	public Guid? UserId { get; set; }
	[ForeignKey(nameof(UserId))]
	public UserDbModel? User { get; set; }

	public Guid? ServerId { get; set; }
	[ForeignKey(nameof(ServerId))]
	public ServerDbModel? Server { get; set; }

	public long? ChannelMessageId { get; set; }
	public Guid? TextChannelId { get; set; }
	[ForeignKey(nameof(ChannelMessageId) + "," + nameof(TextChannelId))]
	public ClassicChannelMessageDbModel? ChannelMessage { get; set; }

	public long? ChatMessageId { get; set; }
	public Guid? ChatId { get; set; }
	[ForeignKey(nameof(ChatMessageId) + "," + nameof(ChatId))]
	public ClassicChatMessageDbModel? ChatMessage { get; set; }

	public Guid? ChatIcId { get; set; }
	[ForeignKey(nameof(ChatIcId))]
	public ChatDbModel? Chat { get; set; }
}
