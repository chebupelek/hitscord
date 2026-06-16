using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace hitscord.Models.db;

public class LessonChannelMessageDbModel
{
    public LessonChannelMessageDbModel()
    {
        CreatedAt = DateTime.UtcNow;
		RealId = Guid.NewGuid();
	}
	[Key]
	public Guid RealId { get; set; }
    public required long Id { get; set; }
    public DateTime CreatedAt { get; set; }

	public Guid? AuthorId { get; set; }
	[ForeignKey(nameof(AuthorId))]
	public UserDbModel? Author { get; set; }

	public required Guid TextLessonChannelId { get; set; }
	[ForeignKey(nameof(TextLessonChannelId))]
	public TextLessonChannelDbModel TextLessonChannel { get; set; }

	public ICollection<FileDbModel> Files { get; set; }

	public long? ReplyToMessageId { get; set; }

    public DateTime? DeleteTime { get; set; }

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
				LessonChannelMessageTaskDbModel => "Task",
				LessonChannelMessageSolutionDbModel => "Solution",
				_ => "Unknown"
			};

			return _messageType;
		}
		private set => _messageType = value;
	}
}