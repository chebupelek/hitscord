using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace hitscord.Models.db;

public class ChannelMessageDbModel
{
    public ChannelMessageDbModel()
    {
        CreatedAt = DateTime.UtcNow;
    }
    public required long Id { get; set; }
    public DateTime CreatedAt { get; set; }

	public Guid AuthorId { get; set; }
	[ForeignKey(nameof(AuthorId))]
	public UserDbModel Author { get; set; }

	[Required]
	public required Guid TextChannelId { get; set; }
	[ForeignKey(nameof(TextChannelId))]
	public TextChannelDbModel? TextChannel { get; set; }

	public long? ReplyToMessageId { get; set; }

    public DateTime? DeleteTime { get; set; }

	[NotMapped]
	private string? _messageType;

	public required List<Guid> TaggedUsers { get; set; }
	public required List<Guid> TaggedRoles { get; set; }

	[NotMapped]
	public string? MessageType
	{
		get
		{
			if (_messageType != null)
				return _messageType;

			_messageType = this switch
			{
				ClassicChannelMessageDbModel => "Classic",
				ChannelVoteDbModel => "Vote",
				_ => "Unknown"
			};

			return _messageType;
		}
		private set => _messageType = value;
	}
}