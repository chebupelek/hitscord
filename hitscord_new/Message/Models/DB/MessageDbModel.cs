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
    public DateTime CreatedAt { get; set; }
    public required Guid UserId { get; set; }
    public required Guid TextChannelId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    [ForeignKey(nameof(ReplyToMessageId))]
    public MessageDbModel? ReplyToMessage { get; set; }
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
				ClassicMessageDbModel => "Classic",
				VoteDbModel => "Vote",
				_ => "Unknown"
			};

			return _messageType;
		}
		private set => _messageType = value;
	}
}