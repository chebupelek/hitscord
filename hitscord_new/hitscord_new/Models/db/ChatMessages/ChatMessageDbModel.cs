using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace hitscord.Models.db;

public class ChatMessageDbModel
{
    public ChatMessageDbModel()
    {
        CreatedAt = DateTime.UtcNow;
		RealId = Guid.NewGuid();
	}
	[Key]
	public Guid RealId { get; set; }
	public required long Id { get; set; }
    public DateTime CreatedAt { get; set; }

	public Guid AuthorId { get; set; }
	[ForeignKey(nameof(AuthorId))]
	public UserDbModel Author { get; set; }

	[Required]
	public required Guid ChatId { get; set; }
	[ForeignKey(nameof(ChatId))]
	public ChatDbModel? Chat { get; set; }

	public required Guid ChatIdDouble { get; set; }

	public long? ReplyToMessageId { get; set; }

    public DateTime? DeleteTime { get; set; }

	public required List<Guid> TaggedUsers { get; set; }

	[NotMapped]
	private string? _messageType;

	[NotMapped]
	public string? MessageType
	{
		get
		{
			if (_messageType != null)
				return _messageType;

			_messageType = this switch
			{
				ClassicChatMessageDbModel => "Classic",
				ChatVoteDbModel => "Vote",
				_ => "Unknown"
			};

			return _messageType;
		}
		private set => _messageType = value;
	}
}